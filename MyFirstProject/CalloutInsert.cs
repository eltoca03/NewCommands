using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Interop;
using MyFirstProject;


[assembly: CommandClass(typeof(MyFirstProject.CalloutInsert))]

namespace MyFirstProject
{
  class CalloutInsert
  {
    // Get the current document and database
    Document acDoc = Application.DocumentManager.MdiActiveDocument;
    Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

    [CommandMethod("COUT")]

    public void cout()
    {
      

      Transaction trans = acCurDb.TransactionManager.StartTransaction();

      using (trans)
      {

        // Open the Block table for read
        BlockTable acBlkTbl;
        acBlkTbl = trans.GetObject(acCurDb.BlockTableId,
                                      OpenMode.ForRead) as BlockTable;

        // Open the Block table record Model space for write
        BlockTableRecord acBlkTblRec;
        acBlkTblRec = trans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                        OpenMode.ForWrite) as BlockTableRecord;

        Editor ed = acDoc.Editor;
        //Prompt options for running line
        PromptEntityOptions peo = new PromptEntityOptions("Select Running Line");
        peo.SetRejectMessage("Running line not selected");
        peo.AddAllowedClass(typeof(Polyline), false);

        PromptEntityResult perRunningLine = ed.GetEntity(peo);
        if (perRunningLine.Status != PromptStatus.OK)
          return;

        Polyline runningLine = trans.GetObject(perRunningLine.ObjectId, OpenMode.ForRead) as Polyline;

        //prompt for the block
        PromptEntityOptions peo2 = new PromptEntityOptions("Select Block");
        peo2.SetRejectMessage("not a block");
        peo2.AddAllowedClass(typeof(BlockReference), false);

        PromptEntityResult perBlock = ed.GetEntity(peo2);
        if (perBlock.Status != PromptStatus.OK)
          return;

        PromptStringOptions pStrOpts = new PromptStringOptions("\nEnter Line Number: ");
        pStrOpts.AllowSpaces = false;
        PromptResult pStrRes = acDoc.Editor.GetString(pStrOpts);

        BlockReference blkRef = trans.GetObject(perBlock.ObjectId, OpenMode.ForRead) as BlockReference;

        string str = String.Format("{0:0}", runningLine.GetDistAtPoint(blkRef.Position));
        switch (str.Length)
        {
          case 1:
            str = "0+0" + str;
            break;
          case 2:
            str = "0+" + str;
            break;
          default:
            str = str.Substring(0, str.Length - 2) + "+" + str.Substring(str.Length - 2);
            break;
        }
        str = str + " LINE " + pStrRes.StringResult;

        Leader leader = new Leader();
        leader.SetDatabaseDefaults();
        leader.AppendVertex(new Point3d(blkRef.Position.X + 1.5, blkRef.Position.Y + 1.5, 0));
        leader.AppendVertex(new Point3d(blkRef.Position.X + 10, blkRef.Position.Y + 5, 0));

        MText txt = new MText();
        txt.SetDatabaseDefaults();
        txt.Location = new Point3d(leader.EndPoint.X + 1.33, leader.EndPoint.Y + 11.1833, 0);
        txt.Contents = "STA " + str;
        txt.Layer = "TEXT-2";
        txt.Attachment = AttachmentPoint.BottomLeft;
        txt.TextHeight = 2.2;
        txt.Layer = "TEXT-2";

        

        MText acMText = new MText();
        acMText.SetDatabaseDefaults();
        acMText.Location = new Point3d(leader.EndPoint.X + 1.65, leader.EndPoint.Y + 4.95, 0);
        acMText.Contents = "PLACE TAP PEDESTAL\r\n" +
                           "10\"%%c x 17\"h 18\" STAKE";
        acMText.Attachment = AttachmentPoint.MiddleLeft;
        acMText.TextHeight = 2.2;
        acMText.Layer = "TEXT-2";

        CoordinateSystem3d cs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
        Plane plane = new Plane(Point3d.Origin, cs.Zaxis);

        Polyline rec = new Polyline();
        rec.AddVertexAt(0, leader.EndPoint.Convert2d(plane),0,0,0);
        rec.AddVertexAt(0, new Point2d(leader.EndPoint.X + 45, leader.EndPoint.Y), 0, 0, 0);
        rec.AddVertexAt(0, new Point2d(leader.EndPoint.X + 45, leader.EndPoint.Y + 10), 0, 0, 0);
        rec.AddVertexAt(0, new Point2d(leader.EndPoint.X, leader.EndPoint.Y + 10), 0, 0, 0);
        rec.Closed = true;

        rec.SetDatabaseDefaults();
        //(
          //  new Point3d(leader.EndPoint.X, leader.EndPoint.Y + 50, 0),
          //  new Point3d(leader.EndPoint.X +100, leader.EndPoint.Y + 50, 0),
          //  new Point3d(leader.EndPoint.X, leader.EndPoint.Y, 0),
          //  new Point3d(leader.EndPoint.X+ 100, leader.EndPoint.Y, 0)
          
          //);
        
        

        acBlkTblRec.AppendEntity(leader);
        trans.AddNewlyCreatedDBObject(leader, true);

        acBlkTblRec.AppendEntity(acMText);
        trans.AddNewlyCreatedDBObject(acMText, true);

        acBlkTblRec.AppendEntity(rec);
        trans.AddNewlyCreatedDBObject(rec, true);

        acBlkTblRec.AppendEntity(txt);
        trans.AddNewlyCreatedDBObject(txt, true);


        trans.Commit();
      }
    }
  }
}
