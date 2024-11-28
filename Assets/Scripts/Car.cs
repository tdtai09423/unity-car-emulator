using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Script
{
    public class Car
    {
        public float RightWheel { get; set; }
        public float LeftWheel { get; set; }
        public float Duration { get; set; }

        public Car(float RightWheel, float LeftWheel, float Duration)
        {
            RightWheel = RightWheel;
            Duration = Duration;
            LeftWheel = LeftWheel;
        }

        public Car() { }

        public void SetDuration(float duration = Mathf.Infinity)
        {
            Duration = duration;
        }

    }
}