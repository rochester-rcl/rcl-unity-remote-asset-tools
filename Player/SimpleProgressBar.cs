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
        private Color errorColor = new Color(0.9f, 0.0f, 0.0f, 1.0f);
        private Image progressImage;
        public bool ErrorState
        {
            get { return errorState; }
            set
            {
                if (value)
                {
                    if (progressImage)
                    {
                        progressImage.color = errorColor;
                    }
                }
                else
                {
                    if (progressImage)
                    {
                        progressImage.color = initialColor;
                    }
                }
                errorState = value;
            }
        }
        public GameObject progressObj;
        public GameObject messageObj;
        public GameObject progressContainer;
        [Tooltip("Animates progress to show the bar is still active.")]
        public bool active = false;
        private GameObject activeObj;
        private Image activeImage;
        private Text messageText;
        private bool errorState;
        private bool animationTriggered;
        private float padding = 0.001f;
        private Color activeColor = new Color(0.8f, 0.8f, 0.8f, 0.25f);
        private Color initialColor;
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
            initialColor = progressImage.color;
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

        public void Toggle(bool val)
        {
            if (progressContainer)
            {
                progressContainer.SetActive(val);
            }
            else
            {
                progressObj.SetActive(val);
                messageObj.SetActive(val);
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

