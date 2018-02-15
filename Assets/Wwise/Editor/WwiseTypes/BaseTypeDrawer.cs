using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AK.Wwise.Editor
{
	public abstract class BaseTypeDrawer : PropertyDrawer
	{
		protected SerializedProperty[] m_guidProperty;  //all components have 1 guid except switches and states which have 2. Index zero is value guid and index 1 is group guid
		protected SerializedProperty[] m_IDProperty;  //all components have 1 ID except switches and states which have 2. Index zero is ID and index 1 is groupID
		protected AkWwiseProjectData.WwiseObjectType m_objectType;
		protected string m_typeName;

		private Rect m_pickerPos = new Rect();
		private Rect m_pressedPosition = new Rect();
		private bool m_buttonWasPressed = false;
		private SerializedObject m_serializedObject;

		public abstract string UpdateIds(Guid[] in_guid);
		public abstract void SetupSerializedProperties(SerializedProperty property);


		private AkDragDropData GetAkDragDropData()
		{
			AkDragDropData DDData = DragAndDrop.GetGenericData(AkDragDropHelper.DragDropIdentifier) as AkDragDropData;
			return (DDData != null && DDData.typeName.Equals(m_typeName)) ? DDData : null;
		}

		private void HandleDragAndDrop(UnityEngine.Event currentEvent, Rect dropArea)
		{
			if (currentEvent.type == EventType.DragExited)
			{
				// clear dragged data
				DragAndDrop.PrepareStartDrag();
			}
			else if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
			{
				if (dropArea.Contains(currentEvent.mousePosition))
				{
					var DDData = GetAkDragDropData();

					if (currentEvent.type == EventType.DragUpdated)
					{
						DragAndDrop.visualMode = DDData != null ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
					}
					else
					{
						DragAndDrop.AcceptDrag();

						if (DDData != null)
						{
							AkUtilities.SetByteArrayProperty(m_guidProperty[0], DDData.guid.ToByteArray());
							m_IDProperty[0].intValue = DDData.ID;

							AkDragDropGroupData DDGroupData = DDData as AkDragDropGroupData;
							if (DDGroupData != null)
							{
								if (m_guidProperty.Length > 1)
									AkUtilities.SetByteArrayProperty(m_guidProperty[1], DDGroupData.groupGuid.ToByteArray());
								if (m_IDProperty.Length > 1)
									m_IDProperty[1].intValue = DDGroupData.groupID;
							}

							//needed for the undo operation to work
							GUIUtility.hotControl = 0;
						}
					}
					currentEvent.Use();
				}
			}
        }

        protected virtual void SetEmptyComponentName(ref string componentName, ref GUIStyle style)
        {
            componentName = "No " + m_typeName + " is currently selected";
            style.normal.textColor = Color.red;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			SetupSerializedProperties(property);

			// Draw label
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			/************************************************Update Properties**************************************************/
			Guid[] componentGuid = new Guid[m_guidProperty.Length];
			for (int i = 0; i < componentGuid.Length; i++)
			{
				byte[] guidBytes = AkUtilities.GetByteArrayProperty(m_guidProperty[i]);
				componentGuid[i] = guidBytes == null ? Guid.Empty : new Guid(guidBytes);
			}

			string componentName = UpdateIds(componentGuid);
			/*******************************************************************************************************************/


			/********************************************Draw GUI***************************************************************/
			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.MiddleLeft;
			style.fontStyle = FontStyle.Normal;

			if (string.IsNullOrEmpty(componentName))
			{
                SetEmptyComponentName(ref componentName, ref style);
            }

			if (GUI.Button(position, componentName, style))
			{
				m_pressedPosition = position;
				m_buttonWasPressed = true;

				// We don't want to set object as dirty only because we clicked the button.
				// It will be set as dirty if the wwise object has been changed by the tree view.
				GUI.changed = false;
			}

			var currentEvent = UnityEngine.Event.current;

			if (currentEvent.type == EventType.Repaint && m_buttonWasPressed && m_pressedPosition.Equals(position))
			{
				m_serializedObject = property.serializedObject;
				m_pickerPos = AkUtilities.GetLastRectAbsolute(false);

				EditorApplication.delayCall += DelayCreateCall;
				m_buttonWasPressed = false;
			}

			HandleDragAndDrop(currentEvent, position);

			EditorGUI.EndProperty();
		}

		private void DelayCreateCall()
		{
			AkWwiseComponentPicker.Create(m_objectType, m_guidProperty, m_IDProperty, m_serializedObject, m_pickerPos);
		}
	}
}