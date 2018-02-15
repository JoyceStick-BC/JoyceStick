using System;
using UnityEditor;

namespace AK.Wwise.Editor
{
	[CustomPropertyDrawer(typeof(Trigger))]
	public class TriggerDrawer : BaseTypeDrawer
	{
		public override string UpdateIds(Guid[] in_guid)
		{
			var list = AkWwiseProjectInfo.GetData().TriggerWwu;

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
			m_objectType = AkWwiseProjectData.WwiseObjectType.TRIGGER;
			m_typeName = "Trigger";

			m_IDProperty = new SerializedProperty[1];
			m_IDProperty[0] = property.FindPropertyRelative("ID");

			m_guidProperty = new SerializedProperty[1];
			m_guidProperty[0] = property.FindPropertyRelative("valueGuid.Array");
		}
	}
}