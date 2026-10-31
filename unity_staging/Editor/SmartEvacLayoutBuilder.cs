using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Copy into Assets/Editor in your Unity project. No gameplay packages required.
public static class SmartEvacLayoutBuilder
{
    private static Material floor, wall, accent, hazard, crowd;

    [MenuItem("Smart-Evac/Build Layout Blockout")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // Check before modifying either project settings or the current scene.
        string[] layers = { "Signage_UI", "Crowd_NPC", "Hazard_Fire", "Hazard_Smoke", "Trigger_Zones" };
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var slots = tags.FindProperty("layers");
        for (int i = 0; i < layers.Length; i++)
        {
            string existing = slots.GetArrayElementAtIndex(i + 8).stringValue;
            if (existing != "" && existing != layers[i])
            {
                Debug.LogError("Smart-Evac: layer " + (i + 8) + " is already occupied by " + existing + ". Resolve layer assignments first.");
                return;
            }
        }
        for (int i = 0; i < layers.Length; i++) slots.GetArrayElementAtIndex(i + 8).stringValue = layers[i];
        tags.ApplyModifiedProperties();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        if (!AssetDatabase.IsValidFolder("Assets/SmartEvac")) AssetDatabase.CreateFolder("Assets", "SmartEvac");
        floor = MaterialAsset("Floor", new Color(.22f, .25f, .28f));
        wall = MaterialAsset("Wall", new Color(.65f, .68f, .70f));
        accent = MaterialAsset("Guidance", new Color(.1f, .85f, .45f));
        hazard = MaterialAsset("HazardPlaceholder", new Color(.95f, .25f, .05f));
        crowd = MaterialAsset("Crowd", new Color(.2f, .4f, .65f));

        Transform environment = Group("[Environment]");
        Transform hall = Group("MainHallway", environment);
        Box("Floor", hall, new Vector3(0, -.15f, -12), new Vector3(6, .3f, 18), floor);
        Box("Wall_Left", hall, new Vector3(-3.15f, 1.75f, -12), new Vector3(.3f, 3.5f, 18), wall);
        Box("Wall_Right", hall, new Vector3(3.15f, 1.75f, -12), new Vector3(.3f, 3.5f, 18), wall);
        Box("BackWall", hall, new Vector3(0, 1.75f, -21.15f), new Vector3(6.6f, 3.5f, .3f), wall);
        Transform intersection = Group("Intersection_Node", environment);
        Box("Floor", intersection, new Vector3(0, -.15f, 0), new Vector3(6, .3f, 6), floor);
        Trigger("Intersection_TriggerZone", intersection, new Vector3(0, 1.5f, 0), new Vector3(6, 3, 6));
        Trigger("TrialStart_Trigger", hall, new Vector3(0, 1.5f, -17), new Vector3(6, 3, .3f));

        Transform targets = Group("RouteTargets");
        string[] routes = { "Left", "Center", "Right" };
        Vector3[] directions = { Vector3.left, Vector3.forward, Vector3.right };
        for (int i = 0; i < routes.Length; i++)
        {
            string route = routes[i];
            Vector3 d = directions[i];
            Transform branch = Group("Corridor_" + route, environment);
            branch.rotation = Quaternion.LookRotation(d);
            Box("Floor", branch, new Vector3(0, -.15f, 14), new Vector3(6, .3f, 22), floor);
            Box("Wall_Left", branch, new Vector3(-3.15f, 1.75f, 14), new Vector3(.3f, 3.5f, 22), wall);
            Box("Wall_Right", branch, new Vector3(3.15f, 1.75f, 14), new Vector3(.3f, 3.5f, 22), wall);
            Box("EndWall", branch, new Vector3(0, 1.75f, 25.15f), new Vector3(6.6f, 3.5f, .3f), wall);
            Trigger("Commit_" + route, branch, new Vector3(0, 1.5f, 3.4f), new Vector3(6, 3, .3f));
            Trigger("Exit_Trigger_" + route, branch, new Vector3(0, 1.5f, 24.5f), new Vector3(6, 3, .5f));
            Transform fire = Group("Hazard_Fire_" + route, branch);
            fire.localPosition = new Vector3(0, 0, 19);
            fire.gameObject.layer = 10;
            var marker = Box("VisualPlaceholder", fire, new Vector3(0, .5f, 0), new Vector3(4, 1, 2), hazard);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.layer = 10;
            fire.gameObject.SetActive(false);
            Transform smoke = Group("Hazard_Smoke_" + route, branch);
            smoke.localPosition = new Vector3(0, 1.5f, 17);
            smoke.gameObject.layer = 11;
            smoke.gameObject.SetActive(false);
            Transform target = Group(route + "TrapPoint", targets);
            target.position = d * 23;
        }

        Transform managers = Group("[Managers]");
        foreach (string name in new[] { "HazardManager", "AI_SupervisorAPI", "TelemetryLogger", "ExperimenterController" }) Group(name, managers);
        Transform crowdRoot = Group("[Crowd_Group]");
        Group("NPCHerdingManager", crowdRoot);
        Group("CrowdSpawnOrigin", crowdRoot).position = new Vector3(0, 0, -12);
        for (int i = 0; i < 12; i++)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC_Preview_" + (i + 1).ToString("D2");
            npc.transform.SetParent(crowdRoot, false);
            npc.transform.position = new Vector3((i % 3 - 1) * 1.4f, 1, -14 + (i / 3) * 1.8f);
            npc.GetComponent<Renderer>().sharedMaterial = crowd;
            npc.layer = 9;
        }
        Transform guidance = Group("[Guidance_UI]");
        Transform signs = Group("ModeA_StaticSigns", guidance);
        Box("ExitSign_Plate", signs, new Vector3(0, 2.8f, 2.8f), new Vector3(1.8f, .55f, .12f), accent).layer = 8;
        var label = Group("EXIT_Label", signs).gameObject.AddComponent<TextMesh>();
        label.transform.localPosition = new Vector3(0, 2.8f, 2.72f);
        label.text = "EXIT";
        label.anchor = TextAnchor.MiddleCenter;
        label.characterSize = .17f;
        label.fontSize = 48;
        Transform arrows = Group("ModeB_DynamicArrows", guidance);
        Box("ArrowShaft", arrows, new Vector3(0, .025f, -1), new Vector3(.22f, .05f, 1.6f), accent).layer = 8;
        for (int i = -1; i <= 1; i += 2)
        {
            var wing = Box("ArrowHead", arrows, new Vector3(i * .24f, .025f, -.45f), new Vector3(.18f, .05f, .8f), accent);
            wing.transform.localRotation = Quaternion.Euler(0, i * -40, 0);
            wing.layer = 8;
        }
        arrows.gameObject.SetActive(false);
        var hud = new GameObject("ModeC_HUDCanvas", typeof(Canvas));
        hud.transform.SetParent(guidance, false);
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        hud.SetActive(false);

        Transform player = Group("[Player_FirstPerson]");
        player.position = new Vector3(0, 0, -19);
        var controller = player.gameObject.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = .3f;
        controller.center = new Vector3(0, .9f, 0);
        Transform camera = Group("Main Camera", player);
        camera.localPosition = new Vector3(0, 1.65f, 0);
        camera.gameObject.tag = "MainCamera";
        camera.gameObject.AddComponent<Camera>().farClipPlane = 100;
        camera.gameObject.AddComponent<AudioListener>();
        var light = Group("Blockout_Light").gameObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(55, -30, 0);
        light.intensity = 1.1f;
        RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = environment.gameObject;
        Debug.Log("Smart-Evac blockout ready. Save the scene, then follow docs/UNITY_LAYOUT.md for gameplay wiring.");
    }

    private static Transform Group(string name, Transform parent = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }

    private static void Trigger(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var t = Group(name, parent);
        t.localPosition = position;
        t.gameObject.layer = 12;
        var collider = t.gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/SmartEvac/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
