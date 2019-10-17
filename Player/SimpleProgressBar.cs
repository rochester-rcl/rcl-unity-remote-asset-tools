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
        [Tooltip("Animates progress to show the bar is still active.")]
        public bool active = false;
        private GameObject activeObj;
        private Image activeImage;
        private Image progressImage;
        private Text messageText;
        private bool animationTriggered;
        private float padding = 0.01f;
        private Color activeColor = new Color(0.8f, 0.8f, 0.8f, 0.25f);
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
            if (active)
            {
                activeObj = Instantiate(progressObj, progressObj.transform.position, progressObj.transform.rotation, progressObj.transform);
                activeImage = activeObj.GetComponent<Image>();
                activeImage.color = activeColor;
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
            if (active)
            {
                if (activeImage.fillAmount < (progressImage.fillAmount - padding))
                {
                    activeImage.fillAmount = Mathf.Lerp(activeImage.fillAmount, progressImage.fillAmount, Time.deltaTime * 2.5f);
                    activeImage.color = Color.Lerp(activeImage.color, Color.clear, Time.deltaTime * 0.5f);
                }
                else
                {
                    activeImage.fillAmount = 0.0f;
                    activeImage.color = activeColor;
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

