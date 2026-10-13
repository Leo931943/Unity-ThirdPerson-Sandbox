using UnityEngine;

namespace ThirdPersonDemo
{
    /// <summary>Builds a stylized humanoid and swings its limbs using the player's movement speed.</summary>
    [ExecuteAlways]
    public sealed class ProceduralCharacterRig : MonoBehaviour
    {
        private Transform leftArm, rightArm, leftLeg, rightLeg, torso;
        private ThirdPersonController movement;
        private float phase;

        private void Awake()
        {
            movement = GetComponent<ThirdPersonController>();
            if (transform.Find("Tailored Jacket") != null)
            {
                leftArm = transform.Find("Left Arm"); rightArm = transform.Find("Right Arm");
                leftLeg = transform.Find("Left Leg"); rightLeg = transform.Find("Right Leg");
                torso = transform.Find("Tailored Jacket");
                return;
            }
            BuildVisuals();
        }

        public void EnsureVisualRig()
        {
            if (transform.Find("Tailored Jacket") == null)
                BuildVisuals();
        }

        private static Material MakeMaterial(string name, Color color, float metallic = 0f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .32f);
            return material;
        }

        private GameObject Part(string name, PrimitiveType shape, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            return part;
        }

        private Transform Limb(string name, Vector3 pivot, float upperLength, float lowerLength, float width, Material fabric, Material endMaterial, bool arm)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.localPosition = pivot;
            Part(name + " Upper", PrimitiveType.Capsule, root, new Vector3(0f, -upperLength * .5f, 0f), new Vector3(width, upperLength * .5f, width), fabric);
            var lower = new GameObject(name + " Lower").transform;
            lower.SetParent(root, false);
            lower.localPosition = new Vector3(0f, -upperLength, 0f);
            Part(name + " Lower Mesh", PrimitiveType.Capsule, lower, new Vector3(0f, -lowerLength * .43f, 0f), new Vector3(width * .82f, lowerLength * .48f, width * .82f), fabric);
            Part(arm ? name + " Hand" : name + " Shoe", arm ? PrimitiveType.Sphere : PrimitiveType.Cube, lower,
                arm ? new Vector3(0f, -lowerLength * .82f, 0f) : new Vector3(0f, -lowerLength * .85f, .10f),
                arm ? Vector3.one * width * 1.05f : new Vector3(width * 1.5f, width * .42f, lowerLength * .72f), endMaterial);
            return root;
        }

        private void BuildVisuals()
        {
            Material shirt = MakeMaterial("Character | teal jacket", new Color(.035f,.20f,.23f));
            Material trousers = MakeMaterial("Character | dark trousers", new Color(.075f,.10f,.14f));
            Material skin = MakeMaterial("Character | warm skin", new Color(.73f,.46f,.31f));
            Material shoes = MakeMaterial("Character | dark leather", new Color(.055f,.045f,.04f));
            Material brass = MakeMaterial("Character | brass detail", new Color(.78f,.43f,.12f), .45f);

            Transform oldBody = transform.Find("BodyVisual");
            if (oldBody != null && oldBody.GetComponent<Renderer>() != null) oldBody.GetComponent<Renderer>().enabled = false;
            Transform oldVisor = transform.Find("VisorEyes");
            if (oldVisor != null) oldVisor.gameObject.SetActive(false);

            torso = Part("Tailored Jacket", PrimitiveType.Capsule, transform, new Vector3(0f,.12f,0f), new Vector3(.57f,.43f,.34f), shirt).transform;
            Part("Neck", PrimitiveType.Cylinder, transform, new Vector3(0f,.57f,0f), new Vector3(.105f,.12f,.105f), skin);
            Part("Head", PrimitiveType.Sphere, transform, new Vector3(0f,.78f,0f), new Vector3(.38f,.47f,.34f), skin);
            Part("Hair", PrimitiveType.Sphere, transform, new Vector3(0f,.98f,-.015f), new Vector3(.39f,.28f,.35f), shoes);
            Part("Nose", PrimitiveType.Sphere, transform, new Vector3(0f,.78f,.315f), new Vector3(.075f,.095f,.09f), skin);
            Part("Belt", PrimitiveType.Cylinder, transform, new Vector3(0f,-.17f,0f), new Vector3(.30f,.045f,.30f), shoes);
            Part("Belt Buckle", PrimitiveType.Cube, transform, new Vector3(0f,-.17f,.30f), new Vector3(.075f,.055f,.025f), brass);
            leftArm = Limb("Left Arm", new Vector3(-.36f,.39f,0f), .34f,.34f,.12f,shirt,skin,true);
            rightArm = Limb("Right Arm", new Vector3(.36f,.39f,0f), .34f,.34f,.12f,shirt,skin,true);
            leftLeg = Limb("Left Leg", new Vector3(-.16f,-.34f,0f), .40f,.42f,.15f,trousers,shoes,false);
            rightLeg = Limb("Right Leg", new Vector3(.16f,-.34f,0f), .40f,.42f,.15f,trousers,shoes,false);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (movement == null) return;
            float intensity = Mathf.Clamp01(movement.Speed / Mathf.Max(.01f, movement.sprintSpeed));
            float amplitude = movement.Grounded ? intensity : .12f;
            if (amplitude > .03f) phase += Time.deltaTime * (movement.IsSprinting ? 12f : 8f);
            float swing = Mathf.Sin(phase) * amplitude;
            float blend = 1f - Mathf.Exp(-12f * Time.deltaTime);
            Animate(leftLeg, swing * 34f, blend);
            Animate(rightLeg, -swing * 34f, blend);
            Animate(leftArm, -swing * 28f - amplitude * 7f, blend);
            Animate(rightArm, swing * 28f - amplitude * 7f, blend);
            if (torso != null) torso.localPosition = Vector3.Lerp(torso.localPosition, new Vector3(0f,.12f + Mathf.Abs(swing)*.035f,0f), blend);
        }

        private static void Animate(Transform limb, float angle, float blend)
        {
            if (limb == null) return;
            limb.localRotation = Quaternion.Slerp(limb.localRotation, Quaternion.Euler(angle,0f,0f), blend);
        }
    }
}



