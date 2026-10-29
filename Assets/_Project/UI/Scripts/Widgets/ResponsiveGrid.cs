using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI
{
    /// <summary>
    /// Switches a GridLayoutGroup between authored landscape and portrait metrics so level
    /// cards reflow from three columns to a single column on phones.
    /// </summary>
    public sealed class ResponsiveGrid : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private Vector2 landscapeCell = new Vector2(460f, 340f);
        [SerializeField] private Vector2 portraitCell = new Vector2(900f, 280f);
        [SerializeField] private int landscapeColumns = 3;
        [SerializeField] private int portraitColumns = 1;
        [SerializeField] private Vector2 landscapeSpacing = new Vector2(30f, 30f);
        [SerializeField] private Vector2 portraitSpacing = new Vector2(20f, 20f);
        [SerializeField] private float portraitAspectThreshold = 0.95f;

        private bool? lastPortrait;

        private void OnEnable() => Apply(force: true);

        private void Update()
        {
            if (Screen.height <= 0)
            {
                return;
            }

            bool portraitNow = (float)Screen.width / Screen.height < portraitAspectThreshold;
            if (lastPortrait != portraitNow)
            {
                Apply(force: false);
            }
        }

        public void Apply(bool force)
        {
            if (grid == null)
            {
                grid = GetComponent<GridLayoutGroup>();
            }

            if (grid == null)
            {
                return;
            }

            bool portrait = Screen.height > 0 && (float)Screen.width / Screen.height < portraitAspectThreshold;
            if (!force && lastPortrait == portrait)
            {
                return;
            }

            lastPortrait = portrait;
            grid.cellSize = portrait ? portraitCell : landscapeCell;
            grid.spacing = portrait ? portraitSpacing : landscapeSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = portrait ? portraitColumns : landscapeColumns;
            grid.childAlignment = TextAnchor.MiddleCenter;
        }
    }
}
