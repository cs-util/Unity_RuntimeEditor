using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    

    public class MultiselectDropdown : MonoBehaviour
    {
        public class OptionData
        {
            public string Text;
            public bool IsSelected;
        }

        [SerializeField]
        private TMP_Dropdown m_dropdown = null;

        [SerializeField]
        private TextMeshProUGUI m_label = null;

        private bool m_isExpanded;

        private Toggle[] m_toggles;
        private List<OptionData> m_options;
        public List<OptionData> options
        {
            get { return m_options; }
            set
            {
                m_options = value;
                m_dropdown.options = m_options.Select(opt => new TMP_Dropdown.OptionData(opt.Text)).ToList();   
            }
        }

        private void LateUpdate()
        {
            if(m_isExpanded != m_dropdown.IsExpanded)
            {
                m_isExpanded = m_dropdown.IsExpanded;
                if(m_isExpanded)
                {
                    m_toggles = m_dropdown.GetComponentsInChildren<Toggle>().Where(tog => !tog.name.StartsWith("Item") ).ToArray();
                    for(int i = 0; i < m_toggles.Length; ++i)
                    {
                        Toggle toggle = m_toggles[i];
                        int index = i;
                        toggle.onValueChanged.AddListener(value => OnToggleValueChanged(index, value));
                    }
                }
                else
                {
                    if(m_toggles != null)
                    {
                        for (int i = 0; i < m_toggles.Length; ++i)
                        {
                            Toggle toggle = m_toggles[i];
                            toggle.onValueChanged.RemoveAllListeners();
                        }
                        m_toggles = null;
                    }
                   
                }

            }
        }

        private void OnToggleValueChanged(int index, bool value)
        {
            Debug.Log("Index " + index + " value " + value);
        }
    }
}
