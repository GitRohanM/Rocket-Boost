using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Makes a slider jump to wherever the track is clicked. Unity only drags from the handle,
    /// so a thin track is otherwise hard to hit. Presses that start on the handle are left to the
    /// Slider itself, which keeps its own click-offset so grabbing it off-centre does not snap.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public sealed class SliderClickToValue : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private Slider slider;
        [SerializeField] private RectTransform handle;
        [SerializeField, Min(0f)] private float step = 0.01f;

        private bool claimDrag;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (handle == null && slider != null)
            {
                handle = slider.handleRect;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            claimDrag = false;

            if (slider == null || !slider.interactable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (IsOnHandle(eventData.position, eventData.pressEventCamera))
            {
                return;
            }

            claimDrag = true;
            Apply(eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (claimDrag)
            {
                Apply(eventData.position, eventData.pressEventCamera);
            }
        }

        private bool IsOnHandle(Vector2 screenPoint, Camera eventCamera)
        {
            if (handle == null)
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(handle, screenPoint, eventCamera, out Vector2 local)
                && handle.rect.Contains(local);
        }

        private void Apply(Vector2 screenPoint, Camera eventCamera)
        {
            // The handle's centre travels the full slide area, so that rect is the 0..1 range.
            RectTransform track = handle != null ? (RectTransform)handle.parent : (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, screenPoint, eventCamera, out Vector2 local))
            {
                return;
            }

            float normalized = Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, local.x);
            float value = Mathf.Lerp(slider.minValue, slider.maxValue, normalized);

            if (step > 0f)
            {
                value = Mathf.Round(value / step) * step;
            }

            slider.value = Mathf.Clamp(value, slider.minValue, slider.maxValue);
        }
    }
}
