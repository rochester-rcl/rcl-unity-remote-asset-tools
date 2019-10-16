using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RemoteAssetBundleTools
{
    public class SimpleProgressBar : MonoBehaviour
    {
        public string Message { get; set; }
        public float Progress { get; set; }
        public GameObject progressObj;
        public GameObject messageObj;
        public bool indicating = false;
        private GameObject indicatorObj;
        private Image indicatorImage;
        private Image progressImage;
        private Text messageText;
        private bool animationTriggered;
        private float padding = 0.01f;
        private Color indicatorColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
        public void Start()
        {
            progressImage = progressObj.GetComponent<Image>();
            messageText = messageObj.GetComponent<Text>();
            if (!progressImage)
            {
                throw new MissingComponentException("SimpleProgressBar requires an Image component on the progressObj GameObject");
            }
            if (!messageText)
            {
                throw new MissingComponentException("SimpleProgressBar requires an Image component on the progressObj GameObject");
            }
            progressImage.fillAmount = 0.0f;
            if (indicating)
            {
                indicatorObj = Instantiate(progressObj, progressObj.transform.position, progressObj.transform.rotation, progressObj.transform);
                indicatorImage = indicatorObj.GetComponent<Image>();
                indicatorImage.color = indicatorColor;
            }
        }

        private void UpdateProgress()
        {
            if (progressImage.fillAmount < Progress)
            {
                progressImage.fillAmount = Mathf.Lerp(progressImage.fillAmount, Progress, Time.deltaTime * 5.0f);
            }
        }

        private void UpdateIndicator()
        {
            if (indicating)
            {
                if (indicatorImage.fillAmount < (progressImage.fillAmount - padding))
                {
                    indicatorImage.fillAmount = Mathf.Lerp(indicatorImage.fillAmount, progressImage.fillAmount, Time.deltaTime * 2.5f);
                    indicatorImage.color = Color.Lerp(indicatorImage.color, Color.clear, Time.deltaTime * 0.5f);
                }
                else
                {
                    indicatorImage.fillAmount = 0.0f;
                    indicatorImage.color = indicatorColor;
                }
            }
        }

        public void Update()
        {
            UpdateProgress();
            UpdateIndicator();
            messageText.text = Message;
        }
    }
}

