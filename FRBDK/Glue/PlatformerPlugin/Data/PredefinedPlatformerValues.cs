﻿using FlatRedBall.PlatformerPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.PlatformerPlugin.Data
{
    public static class PredefinedPlatformerValues
    {
        static Dictionary<string, PlatformerValuesViewModel> platformerValues =
            new Dictionary<string, PlatformerValuesViewModel>();
        static PredefinedPlatformerValues()
        {
            {
                var unnamed = new PlatformerValuesViewModel
                {
                    Name = "Unnamed",
                };
                platformerValues.Add(unnamed.Name, unnamed);
            }


            {
                var defaultGround = new PlatformerValuesViewModel
                {
                    Name = "Ground",

                    MaxSpeedX = 250,
                    AccelerationTimeX = .25f,
                    DecelerationTimeX = .15f,
                    IsImmediate = false,
                    JumpVelocity = 450,
                    JumpApplyByButtonHold = true,
                    JumpApplyLength = .2f,
                    Gravity = 900,
                    MaxFallSpeed = 500,
                };
                platformerValues.Add(defaultGround.Name, defaultGround);
            }

            {
                var defaultInAir = new PlatformerValuesViewModel
                {
                    Name = "Air",

                    MaxSpeedX = 250,
                    AccelerationTimeX = 1,
                    DecelerationTimeX = 1,
                    IsImmediate = false,
                    JumpVelocity = 0,
                    Gravity = 900,
                    MaxFallSpeed = 500
                };
                platformerValues.Add(defaultInAir.Name, defaultInAir);
            }
        }

        public static PlatformerValuesViewModel GetValues(string name)
        {
            var toReturn = platformerValues[name].Clone();
            return toReturn;
        }
    }
}
