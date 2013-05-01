using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VMath;
using VVVV.Utils.VColor;

namespace DrawSmoother
{
    /// <summary>
    /// One particle
    /// </summary>
    public class Particle
    {
        /// <summary>
        /// Координаты сглаженной точки
        /// </summary>
        public float x, y;
        /// <summary>
        /// Компоненты треугольника
        /// </summary>
        public float x1, y1, x2, y2;
        /// <summary>
        /// Конструктор
        /// </summary>
        public Particle() { }

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        public Particle(float _x, float _y)
        {
            x = _x;
            y = _y;
            x1 = y1 = x2 = y2 = -1f;
        }
    }

}
