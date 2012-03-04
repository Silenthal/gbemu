using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GBEmu.Render
{
	interface IRenderable
	{
		void CopyData(byte[] buffer);
	}
}
