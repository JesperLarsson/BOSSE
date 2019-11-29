/*
 * Copyright Jesper Larsson 2019, Linköping, Sweden
 */
namespace DebugGui
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using BOSSE;
    using SC2APIProtocol;

    /// <summary>
    /// Base class for drawable minimaps
    /// </summary>
    public abstract class BaseMap
    {
        protected Graphics FormGraphics;
        protected int BaseX;
        protected int BaseY;

        public BaseMap(Graphics _formGraphics, int _baseX, int _baseY)
        {
            FormGraphics = _formGraphics;
            BaseX = _baseX;
            BaseY = _baseY + 10;
        }
    }
}
