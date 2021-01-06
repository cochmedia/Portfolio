using UnityEngine;
using System.Collections;
using System.IO;

namespace EMAX.Hardware.MotionPlatform
{
    public class StageDriver : MonoBehaviour {

        public bool DebugLogInfo;

        public Vector4 BalanceStroke =  new Vector4(50,50,50,50);

        public float RollMaximumAngel = 30;
        public float RollMaximumStroke = 30;

        public float PitchMaximumAngel = 30;
        public float PitchMaximumStroke = 30;

        [Range(0.4f,1)]
        public float DampingTime = 0.6f;

        private Vector3 initAngle = Vector3.zero;

        private float rollStroke;
        private float pitchStroke;

        private float currentAxis3Stroke;

        void Start()
        {
            //init hardware
            MotionPlatformController.Init();
        }


        //release hardware
        public void OnDestroy()
        {
            MotionPlatformController.ReleaseDevice();
            MotionPlatformController.Release(0, "");
        }

        //update every frame
        void Update()
        {
            axis3Rotate();
        }

        //calculate strokes for axes
        void axis3Rotate()
        {
            calculateAngel();

            float axisLeft = BalanceStroke.x - rollStroke / 2 + pitchStroke / 2;
            float axisRight = BalanceStroke.y + rollStroke / 2 + pitchStroke / 2;
            float axisFront = BalanceStroke.z - pitchStroke / 2;

            float axis1Stroke = axisLeft;
            float axis2Stroke = axisRight;
            float axis3Stroke = axisFront;

            if (DebugLogInfo) Debug.LogFormat("eulerAngles:{0} | roll:{1} pitch:{2} | left:{3} right:{4} front:{5}", transform.eulerAngles.ToString("f3"),rollStroke, pitchStroke, axis1Stroke, axis2Stroke, axis3Stroke);

            MotionPlatformController.Move(axis2Stroke, axis1Stroke, axis3Stroke, 0, 0);
	    }
        
        //calculate pitch and roll according to rotation
        void calculateAngel()
        {
        
            float targetX = transform.eulerAngles.x;
            if (targetX > 180)
            {
                targetX = -(360 - targetX);
            }

            float targetZ = transform.eulerAngles.z;
            if (targetZ > 180)
            {
                targetZ = -(360 - targetZ);
            }

            rollStroke = (targetZ - initAngle.z) / RollMaximumAngel * RollMaximumStroke;
            pitchStroke = (targetX - initAngle.x) / PitchMaximumAngel * PitchMaximumStroke;
        }
    }

}