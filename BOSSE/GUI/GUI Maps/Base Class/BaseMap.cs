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
        protected int RenderScale;
        protected Graphics FormGraphics;
        protected int BaseX;
        protected int BaseY;

        public BaseMap(Graphics _formGraphics, int _baseX, int _baseY, int _renderScale)
        {
            FormGraphics = _formGraphics;
            BaseX = _baseX;
            BaseY = _baseY + 10;
            RenderScale = _renderScale;
        }

        /// <summary>
        /// We need to compensate Y coordinates, game uses from the bottom left, and GUI from the top left
        /// </summary>
        protected float CompensateY(float y)
        {
            var playArea = BosseGui.GameInformation.StartRaw.PlayableArea;

            float temp = playArea.P1.Y - y - playArea.P0.Y;
            return temp;
        }

        public virtual void Tick()
        {

        }
    }
}
