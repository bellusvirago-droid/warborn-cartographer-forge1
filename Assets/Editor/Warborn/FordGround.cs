using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// THE WARBORN MARCH - CARTOGRAPHER'S DRAWING
/// The Sundered Ford Environment Generator
/// 
/// Headless-compatible, idempotent generator for the Phase II vertical slice battlefield.
/// Carves the high ground east, the diggable ground west, and the river cut. 
/// Generates deterministic scatter, rules-based splatmaps, and a URP river shader 
/// adhering to the Stillness setting (<3Hz motion).
/// </summary>
public static class FordGround
{
    private const string GENERATED_PATH = "Assets/Generated/Ford";
    private const string TERRAIN_NAME = "SunderedFord_Terrain";
    private const string RIVER_NAME = "SunderedFord_River";
    private const int SEED = 42; // Deterministic seed for scatter and noise

    // Survey-accurate hex colours for physical albedo
    private static readonly Color COLOR_MUD = HexToColor("#3d332a");
    private static readonly Color COLOR_GRASS = HexToColor("#253325");
    private static readonly Color COLOR_SHALE = HexToColor("#4a4742");

    [MenuItem("TheMarch/Generate Sundered Ford")]
    public static void BuildFordHeadless()
    {
        Debug.Log("[FordGround] Commencing headless terrain generation for the Sundered Ford...");

        EnsureDirectories();
        
        // 1. Generate core materials and textures to guarantee no magenta fallbacks
        TerrainLayer[] layers = GenerateTerrainLayers();
        
        // 2. Sculpt heightmap and paint splatmaps
        TerrainData tData = GenerateTerrainData(layers);
        
        // 3. Instantiate or update the Terrain GameObject
        SpawnTerrain(tData);
        
        // 4. Generate the URP River Shader and Material
        Material riverMat = GenerateRiverMaterial();
        
        // 5. Instantiate the River surface
        SpawnRiver(riverMat);
        
        // 6. Deterministic scatter of debris/rocks
        ScatterDebris();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[FordGround] Sundered Ford generated successfully.");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(GENERATED_PATH))
            AssetDatabase.CreateFolder("Assets/Generated", "Ford");
    }

    private static TerrainLayer[] GenerateTerrainLayers()
    {
        // Mud at the water, Grass on the banks, Shale in the ford/cliffs
        TerrainLayer mud = CreateLayer("MudLayer", COLOR_MUD, 0.92f, 0.02f);
        TerrainLayer grass = CreateLayer("GrassLayer", COLOR_GRASS, 0.90f, 0.02f);
        TerrainLayer shale = CreateLayer("ShaleLayer", COLOR_SHALE, 0.86f, 0.04f);
        return new TerrainLayer[] { mud, grass, shale };
    }

    private static TerrainLayer CreateLayer(string name, Color albedo, float roughness, float metallic)
    {
        string path = $"{GENERATED_PATH}/{name}.terrainlayer";
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }

        // Procedurally generate a base texture to ensure no missing dependencies
        string texPath = $"{GENERATED_PATH}/{name}_Albedo.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            tex = new Texture2D(256, 256, TextureFormat.RGBA32, true, true);
            Color[] pixels = new Color[256 * 256];
            System.Random prng = new System.Random(name.GetHashCode());
            for (int i = 0; i < pixels.Length; i++)
            {
                // Slight deterministic noise for albedo variance
                float noise = (float)(prng.NextDouble() * 0.1 - 0.05);
                pixels[i] = new Color(
                    Mathf.Clamp01(albedo.r + noise),
                    Mathf.Clamp01(albedo.g + noise),
                    Mathf.Clamp01(albedo.b + noise),
                    1.0f
                );
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Application.dataPath + texPath.Substring(6), tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(10, 10);
        layer.smoothness = 1.0f - roughness;
        layer.metallic = metallic;
        
        EditorUtility.SetDirty(layer);
        return layer;
    }

    private static TerrainData GenerateTerrainData(TerrainLayer[] layers)
    {
        string path = $"{GENERATED_PATH}/SunderedFordData.asset";
        TerrainData tData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
        if (tData == null)
        {
            tData = new TerrainData();
            AssetDatabase.CreateAsset(tData, path);
        }

        tData.heightmapResolution = 513;
        tData.alphamapResolution = 512;
        tData.size = new Vector3(200, 30, 200); // 200x200m battlefield
        tData.terrainLayers = layers;

        float[,] heights = new float[tData.heightmapResolution, tData.heightmapResolution];
        
        // The heightmap rules:
        // West (x < 0.45): Diggable, soft, low ground (~0.2)
        // Middle (0.45 - 0.55): The River cut, lowest point (~0.05)
        // East (x > 0.55): High ground, sharp incline (~0.6)
        for (int y = 0; y < tData.heightmapResolution; y++)
        {
            for (int x = 0; x < tData.heightmapResolution; x++)
            {
                float nx = (float)x / tData.heightmapResolution;
                float ny = (float)y / tData.heightmapResolution;
                
                // Base deterministic noise
                float noise = Mathf.PerlinNoise(nx * 5f + SEED, ny * 5f + SEED) * 0.05f;
                float h = 0.2f + noise;

                // Meandering river offset
                float riverMeander = Mathf.Sin(ny * Mathf.PI * 2) * 0.05f;
                float riverCenter = 0.5f + riverMeander;
                
                float distToRiver = Mathf.Abs(nx - riverCenter);
                
                if (distToRiver < 0.05f)
                {
                    // River bed dip
                    h = 0.05f + (distToRiver * 2f) + (noise * 0.2f);
                }
                else if (nx > riverCenter)
                {
                    // Eastern High Ground
                    float eastProgression = Mathf.Clamp01((nx - (riverCenter + 0.05f)) / 0.2f);
                    h = Mathf.Lerp(h, 0.6f + noise + (Mathf.PerlinNoise(nx * 10f, ny * 10f) * 0.1f), eastProgression);
                }
                
                heights[y, x] = h;
            }
        }
        tData.SetHeights(0, 0, heights);

        // Paint Splatmaps based on height and steepness
        float[,,] splat = new float[tData.alphamapResolution, tData.alphamapResolution, layers.Length];
        for (int y = 0; y < tData.alphamapResolution; y++)
        {
            for (int x = 0; x < tData.alphamapResolution; x++)
            {
                float nx = (float)x / tData.alphamapResolution;
                float ny = (float)y / tData.alphamapResolution;
                
                float h = tData.GetHeight(y, x) / tData.size.y;
                float steepness = tData.GetSteepness(nx, ny);
                
                float weightMud = 0f;
                float weightGrass = 0f;
                float weightShale = 0f;

                if (steepness > 25f)
                {
                    weightShale = 1f;
                }
                else if (h < 0.12f)
                {
                    weightMud = 1f;
                }
                else
                {
                    weightGrass = 1f;
                }

                // Blend transitions
                float sum = weightMud + weightGrass + weightShale;
                splat[x, y, 0] = weightMud / sum;
                splat[x, y, 1] = weightGrass / sum;
                splat[x, y, 2] = weightShale / sum;
            }
        }
        tData.SetAlphamaps(0, 0, splat);
        EditorUtility.SetDirty(tData);
        return tData;
    }

    private static void SpawnTerrain(TerrainData tData)
    {
        GameObject existing = GameObject.Find(TERRAIN_NAME);
        if (existing != null) GameObject.DestroyImmediate(existing);

        GameObject terrainGO = Terrain.CreateTerrainGameObject(tData);
        terrainGO.name = TERRAIN_NAME;
        // Center the terrain bounds around 0,0,0 so the ford sits naturally at origin
        terrainGO.transform.position = new Vector3(-tData.size.x / 2, 0, -tData.size.z / 2);
        
        Terrain terrain = terrainGO.GetComponent<Terrain>();
        terrain.basemapDistance = 250;
        terrain.heightmapPixelError = 5;
        terrain.drawInstanced = true; // Crucial for WebGL SRP batching
    }

    private static Material GenerateRiverMaterial()
    {
        string shaderPath = $"{GENERATED_PATH}/RiverDepth.shader";
        
        // Procedurally generating the river shader to ensure exact physical fidelity without manual graph setup.
        // Complies with < 3Hz stillness: Flow speed is capped and smooth.
        string shaderCode = @"
Shader ""TheMarch/FordRiver""
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.05, 0.08, 0.1, 0.9)
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.25, 0.2, 0.7)
        _FoamColor ("Foam Color", Color) = (0.8, 0.8, 0.75, 0.8)
        _DepthMax ("Depth Max", Float) = 2.0
        _FlowSpeed ("Flow Speed", Float) = 0.1
    }
    SubShader
    {
        Tags { ""RenderType""=""Transparent"" ""Queue""=""Transparent-1"" ""RenderPipeline""=""UniversalPipeline"" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name ""ForwardLit""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl""

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                float _DepthMax;
                float _FlowSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogCoord = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float linearEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float screenDepth = input.screenPos.w;
                
                float depthDifference = linearEyeDepth - screenDepth;
                float depthGradient = saturate(depthDifference / _DepthMax);
                
                // Foam calculation (intersection)
                float foamLine = saturate(1.0 - (depthDifference * 4.0));
                
                // Flow noise mock (no looping texture, mathematical pan strictly < 3Hz)
                float timePan = _Time.y * _FlowSpeed;
                float surfaceNoise = sin(input.uv.x * 20.0 + timePan) * cos(input.uv.y * 20.0 + timePan * 0.8);
                
                half4 waterColor = lerp(_ShallowColor, _DeepColor, depthGradient);
                waterColor += (surfaceNoise * 0.05);
                
                half4 finalColor = lerp(waterColor, _FoamColor, foamLine * _FoamColor.a);
                
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);
                return finalColor;
            }
            ENDHLSL
        }
    }
}";
        File.WriteAllText(Application.dataPath + shaderPath.Substring(6), shaderCode);
        AssetDatabase.ImportAsset(shaderPath);
        
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        
        string matPath = $"{GENERATED_PATH}/RiverMat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }
        
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SpawnRiver(Material mat)
    {
        GameObject existing = GameObject.Find(RIVER_NAME);
        if (existing != null) GameObject.DestroyImmediate(existing);

        GameObject riverGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        riverGO.name = RIVER_NAME;
        riverGO.transform.position = new Vector3(0, 3.5f, 0); // Fits the 0.12 normalized height of the riverbed
        riverGO.transform.localScale = new Vector3(20, 1, 20); // 200x200m
        
        // Remove collision, it's just visual
        GameObject.DestroyImmediate(riverGO.GetComponent<Collider>());
        
        riverGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static void ScatterDebris()
    {
        // Clean up old scatter
        GameObject existingGroup = GameObject.Find("SunderedFord_Scatter");
        if (existingGroup != null) GameObject.DestroyImmediate(existingGroup);

        GameObject scatterGroup = new GameObject("SunderedFord_Scatter");
        System.Random prng = new System.Random(SEED);

        // Fallback procedural rock material using shale color
        Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rockMat.color = COLOR_SHALE;
        rockMat.SetFloat("_Smoothness", 0.1f);

        Terrain terrain = GameObject.Find(TERRAIN_NAME)?.GetComponent<Terrain>();
        if (terrain == null) return;

        // Place 100 deterministic rocks
        for (int i = 0; i < 100; i++)
        {
            float nx = (float)prng.NextDouble();
            float nz = (float)prng.NextDouble();
            
            float wx = nx * 200 - 100;
            float wz = nz * 200 - 100;
            
            float wy = terrain.SampleHeight(new Vector3(wx, 0, wz));
            
            // Avoid placing rocks in the deep riverbed, favour the banks and eastern high ground
            if (wy < 3.8f) continue; 

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.transform.SetParent(scatterGroup.transform);
            rock.transform.position = new Vector3(wx, wy, wz);
            
            // Deterministic rotation and scaling
            rock.transform.rotation = Quaternion.Euler(
                (float)prng.NextDouble() * 360f, 
                (float)prng.NextDouble() * 360f, 
                (float)prng.NextDouble() * 360f
            );
            rock.transform.localScale = new Vector3(
                0.5f + (float)prng.NextDouble() * 1.5f,
                0.3f + (float)prng.NextDouble() * 1.0f,
                0.5f + (float)prng.NextDouble() * 1.5f
            );
            
            rock.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            
            // Make static for GI and batching
            rock.isStatic = true;
        }
    }

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
