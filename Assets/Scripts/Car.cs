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
        public float Speed { get; set; }
        public float Angle { get; set; }
        public float Duration { get; set; }

        public Car(float speed, float duration, float angle)
        {
            Speed = speed;
            Duration = duration;
            Angle = angle;
        }

        public Car() { }

        public void SetDuration(float duration = Mathf.Infinity)
        {
            Duration = duration;
        }

    }
}