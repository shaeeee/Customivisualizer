using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customivisualizer
{
	public class UIPanel
	{
		public string Header { get; private set; }

		public delegate void OnDrawDelegate();
		public OnDrawDelegate OnDraw;

		public float Width { get; private set; }

		public UIPanel(string header, OnDrawDelegate onDraw, float width)
		{
			Header = header;
			OnDraw = onDraw;
			Width = width;
		}

		public void Draw()
		{
			OnDraw();
		}
	}
}