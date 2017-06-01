using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Runtime;

namespace MyFirstProject
{
  class Segment
  {
    public String _segType { get; set; }
    public int _verticeNumber { get; set; }

    public Segment(String segType, int verticeNumer)
    {
      _segType = segType;
      _verticeNumber = verticeNumer;

    }

  }
}
