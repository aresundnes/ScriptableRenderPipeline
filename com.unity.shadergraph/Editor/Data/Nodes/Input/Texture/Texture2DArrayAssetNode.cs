using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 2D Array Asset")]
    class Texture2DArrayAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture2DArrayAssetNode()
        {
            name = "Texture 2D Array Asset";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Texture-2D-Array-Asset-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTextureArray m_Texture = new SerializableTextureArray();

        [TextureArrayControl("")]
        public Texture2DArray texture
        {
            get { return m_Texture.textureArray; }
            set
            {
                if (m_Texture.textureArray == value)
                    return;
                m_Texture.textureArray = value;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectGraphInputs(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddGraphInput(new ShaderProperty(PropertyType.Texture2DArray));//()
            // {
            //     overrideReferenceName = GetVariableNameForSlot(OutputSlotId),
            //     generatePropertyBlock = true,
            //     value = m_Texture,
            //     modifiable = false
            // });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture2DArray)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                textureValue = texture
            });
        }

        public ShaderProperty AsShaderProperty()
        {
            var prop = new ShaderProperty(PropertyType.Texture2DArray);// { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
