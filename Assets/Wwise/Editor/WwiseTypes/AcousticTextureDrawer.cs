using System;
using UnityEditor;
using UnityEngine;

namespace AK.Wwise.Editor
{
	[CustomPropertyDrawer(typeof(AcousticTexture))]
	public class AcousticTextureDrawer : BaseTypeDrawer
    {
        protected override void SetEmptyComponentName(ref string componentName, ref GUIStyle style)
        {
            componentName = "None";
        }

        public override string UpdateIds(Guid[] in_guid)
		{
			var list = AkWwiseProjectInfo.GetData().AcousticTextureWwu;

			for (int i = 0; i < list.Count; i++)
			{
				var element = list[i].List.Find(x => new Guid(x.Guid).Equals(in_guid[0]));

				if (element != null)
				{
					m_IDProperty[0].intValue = element.ID;
					return element.Name;
				}
			}

			m_IDProperty[0].intValue = 0;
			return string.Empty;
		}

		public override void SetupSerializedProperties(SerializedProperty property)
		{
			m_objectType = AkWwiseProjectData.WwiseObjectType.ACOUSTICTEXTURE;
			m_typeName = "AcousticTexture";

			m_IDProperty = new SerializedProperty[1];
			m_IDProperty[0] = property.FindPropertyRelative("ID");

			m_guidProperty = new SerializedProperty[1];
			m_guidProperty[0] = property.FindPropertyRelative("valueGuid.Array");
		}
	}
}