using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using mptcc = Mediapipe.Tasks.Components.Containers;

namespace Mediapipe.Unity
{
    public class PoseLandmarkerResultAnnotationController : AnnotationController<MultiPoseLandmarkListWithMaskAnnotation>
    {
        [SerializeField] private bool _visualizeZ = false;
        [SerializeField] private PoseCubeSpawner cubeSpawner;
        [SerializeField] private UICubeToggle uiCubeToggle;

        private readonly object _currentTargetLock = new object();
        private PoseLandmarkerResult _currentTarget;

        public void InitScreen(int maskWidth, int maskHeight) => annotation.InitMask(maskWidth, maskHeight);

        public void DrawNow(PoseLandmarkerResult target)
        {
            target.CloneTo(ref _currentTarget);
            if (_currentTarget.segmentationMasks != null)
            {
                ReadMask(_currentTarget.segmentationMasks);
                _currentTarget.segmentationMasks.Clear();
            }
            SyncNow();
        }

        public void DrawLater(PoseLandmarkerResult target) => UpdateCurrentTarget(target);

        private void ReadMask(IReadOnlyList<Image> segmentationMasks) => annotation.ReadMask(segmentationMasks, isMirrored);

        protected void UpdateCurrentTarget(PoseLandmarkerResult newTarget)
        {
            lock (_currentTargetLock)
            {
                newTarget.CloneTo(ref _currentTarget);
                if (_currentTarget.segmentationMasks != null)
                {
                    ReadMask(_currentTarget.segmentationMasks);
                    _currentTarget.segmentationMasks.Clear();
                }
                isStale = true;
            }
        }

        protected override void SyncNow()
        {
            lock (_currentTargetLock)
            {
                isStale = false;

                bool personDetected = _currentTarget.poseLandmarks != null && _currentTarget.poseLandmarks.Count > 0;

                if (personDetected)
                {
                    if (annotation != null)
                        annotation.gameObject.SetActive(false);

                    if (uiCubeToggle != null)
                        uiCubeToggle.SetPersonDetected(true);

                    if (cubeSpawner != null && cubeSpawner.HasCube)
                    {
                        mptcc.NormalizedLandmarks normalizedWrap = _currentTarget.poseLandmarks[0];
                        IReadOnlyList<mptcc.NormalizedLandmark> nls = normalizedWrap.landmarks;

                        var landmarks = new List<Mediapipe.Landmark>(nls.Count);
                        for (int i = 0; i < nls.Count; i++)
                        {
                            var nl = nls[i];
                            landmarks.Add(new Mediapipe.Landmark
                            {
                                X = nl.x,
                                Y = nl.y,
                                Z = nl.z
                            });
                        }

                        cubeSpawner.UpdatePose(landmarks);
                        Debug.Log($"[PoseLandmarkerResultAnnotationController] Person detected → forwarding {landmarks.Count} landmarks to CubeSpawner.");
                    }
                }
                else
                {
                    if (cubeSpawner != null)
                        cubeSpawner.DestroyCube();

                    if (annotation != null)
                        annotation.gameObject.SetActive(false);

                    if (uiCubeToggle != null)
                        uiCubeToggle.SetPersonDetected(false);

                    Debug.Log("[PoseLandmarkerResultAnnotationController] No person detected → Cube OFF, Skeleton OFF, Toggle disabled.");
                }
            }
        }
    }
}
