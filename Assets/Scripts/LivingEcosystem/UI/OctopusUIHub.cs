using CreateEnv.Ecosystem.Genetics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CreateEnv.Ecosystem.UI
{
    // Ties the three genetics screens together and turns a tap on an octopus in the
    // world into an inspector card.
    //
    // Tapping in AR is fiddly — the animals are small, they move, and a phone is held
    // one-handed — so there are two ways in: tap the octopus itself, or pick it from
    // the list in the ecosystem panel. Learners who cannot land the tap are exactly
    // the ones who most need the card.
    public class OctopusUIHub : MonoBehaviour
    {
        LivingReefController _reef;

        public OctopusInspectorUI Inspector { get; private set; }
        public BreedingToolUI Breeding { get; private set; }
        public FamilyTreeUI Tree { get; private set; }

        static readonly RaycastHit[] _hits = new RaycastHit[8];

        public static OctopusUIHub Create(Transform canvas, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(canvas, "OctopusUI");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var hub = host.AddComponent<OctopusUIHub>();
            hub._reef = reef;

            hub.Inspector = OctopusInspectorUI.Create(host.transform, reef);
            hub.Breeding = BreedingToolUI.Create(host.transform, reef);
            hub.Tree = FamilyTreeUI.Create(host.transform, reef);

            hub.Inspector.onBreedRequested += id =>
            {
                hub.Inspector.Close();
                hub.Breeding.Open(id);
            };
            hub.Inspector.onTreeRequested += id =>
            {
                hub.Inspector.Close();
                hub.Tree.Open(id);
            };
            hub.Tree.onAnimalTapped += id =>
            {
                hub.Tree.Close();
                hub.Inspector.Open(id);
            };

            return hub;
        }

        public void Inspect(int agentId) => Inspector.Open(agentId);

        void Update()
        {
            if (_reef == null || _reef.Octopuses == null) return;
            if (Inspector.IsOpen) return;

            if (!TryGetTap(out Vector2 screenPoint)) return;

            // A tap that landed on the interface is not a tap on the reef.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var cam = Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(screenPoint);
            int count = Physics.RaycastNonAlloc(ray, _hits, 60f);
            for (int i = 0; i < count; i++)
            {
                var view = _hits[i].collider != null
                    ? _hits[i].collider.GetComponentInParent<OctopusAgentView>()
                    : null;
                if (view == null || view.agentId < 0) continue;

                Inspector.Open(view.agentId);
                return;
            }
        }

        static bool TryGetTap(out Vector2 point)
        {
            point = default;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) { point = Input.mousePosition; return true; }
            return false;
#else
            if (Input.touchCount == 0) return false;
            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began) return false;
            point = touch.position;
            return true;
#endif
        }
    }
}
