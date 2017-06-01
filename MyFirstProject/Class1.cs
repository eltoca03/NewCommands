using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
 
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Interop;
using MyFirstProject;

[assembly: CommandClass(typeof(MyFirstProject1.Class1))]
 
namespace MyFirstProject1
{
  public class Class1
  {

    private const double ELEVENDEGINRAD = 0.191986;
    private SelectionSet _ss;
    private Polyline _runningLine;

    // Get the current document and database
    Document acDoc = Application.DocumentManager.MdiActiveDocument;
    Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

    [CommandMethod("bor")]
    public void bor()
    {
      // Get the current document and database, and start a transaction
      Document acDoc = Application.DocumentManager.MdiActiveDocument;
      Database acCurDb = acDoc.Database;
        
      // Starts a new transaction with the Transaction Manager
      using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
      {
        // Open the Block table record for read
        BlockTable acBlkTbl;
        acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                    OpenMode.ForRead) as BlockTable;
 
        // Open the Block table record Model space for write
        BlockTableRecord acBlkTblRec;
        acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                        OpenMode.ForWrite) as BlockTableRecord;
          
        // Open the Layer table for read
        LayerTable acLyrTbl;
        acLyrTbl = acTrans.GetObject(acCurDb.LayerTableId,
                                    OpenMode.ForRead) as LayerTable;

        // Open the Text Style Table
        TextStyleTable acTxStyTbl;
        acTxStyTbl= acTrans.GetObject(acCurDb.TextStyleTableId, 
                                      OpenMode.ForRead) as TextStyleTable;

        string sLayerName = "Bore";
 
        if (acLyrTbl.Has(sLayerName) == false)
        {
          LayerTableRecord acLyrTblRec = new LayerTableRecord();
 
          // Assign the layer the ACI color 1 and a name
          acLyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
          acLyrTblRec.Name = sLayerName;
 
          // Upgrade the Layer table for write
          acLyrTbl.UpgradeOpen();
 
          // Append the new layer to the Layer table and the transaction
          acLyrTbl.Add(acLyrTblRec);
          acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
        }


        PromptSelectionOptions sel = new PromptSelectionOptions();
        PromptPointResult pPtRes;
        PromptPointOptions pPtOpts = new PromptPointOptions("");

        // Prompt for the start point
        pPtOpts.Message = "\nBegin Bore: ";
        pPtRes = acDoc.Editor.GetPoint(pPtOpts);
        Point3d ptStart = pPtRes.Value;

        Vector3d vectorBeg = new Vector3d(0,1,0);
        //ptStart.Add(vectorBeg);

        // Exit if the user presses ESC or cancels the command
        if (pPtRes.Status == PromptStatus.Cancel) return;

        // Prompt for the end point
        pPtOpts.Message = "\nEnd Bore: ";
        pPtOpts.UseBasePoint = true;
        pPtOpts.BasePoint = ptStart;
        pPtRes = acDoc.Editor.GetPoint(pPtOpts);
        Point3d ptEnd = pPtRes.Value;
            
        if (pPtRes.Status == PromptStatus.Cancel) return;

            
        //
        Line myLine = new Line(ptStart, ptEnd);
        myLine.Layer = sLayerName;
            
        //Append line to model space
        acBlkTblRec.AppendEntity(myLine);

        //append line to transaction
        acTrans.AddNewlyCreatedDBObject(myLine, true);

        //get bore distance
        double boreDist = myLine.Length;

        //Application.ShowAlertDialog(ptStart.ToString() + " , " + ptEnd.ToString() + boreDist);

        //offset the line 
        DBObjectCollection acDbObjColl = myLine.GetOffsetCurves(1);

        Point3d offsetLineMiddle = new Point3d();
        Line topLine = new Line();
        Line bottomLine = new Line();
        double offsetLineAngle = 0;
        
        foreach (Entity acEnt in acDbObjColl)
        {
            // Add each offset object
            acBlkTblRec.AppendEntity(acEnt);
            if (acEnt.GetType() == typeof(Line))
            {
                try
                {
                    Convert.ChangeType(acEnt, typeof(Line));
                    offsetLineMiddle = getMidPoint((Line)acEnt);
                    offsetLineAngle = ((Line)acEnt).Angle;
                    topLine = ((Line)acEnt);
                }
                catch(InvalidCastException)
                {
                    Application.ShowAlertDialog("Cannot convert offset Entity to Line");
                }

            }
            acTrans.AddNewlyCreatedDBObject(acEnt, true);
        }

        //offset the line 
        DBObjectCollection acDbObjColl2 = myLine.GetOffsetCurves(-1);


        foreach (Entity acEnt in acDbObjColl2)
        {
            // Add each offset object
            acBlkTblRec.AppendEntity(acEnt);
            bottomLine = ((Line)acEnt);
            acTrans.AddNewlyCreatedDBObject(acEnt, true);
        }

        Line edgeOne = new Line(topLine.StartPoint, bottomLine.StartPoint);
        edgeOne.Layer = sLayerName;
        acBlkTblRec.AppendEntity(edgeOne);
        acTrans.AddNewlyCreatedDBObject(edgeOne, true);

        Line edgeTwo = new Line(topLine.EndPoint, bottomLine.EndPoint);
        edgeTwo.Layer = sLayerName;
        acBlkTblRec.AppendEntity(edgeTwo);
        acTrans.AddNewlyCreatedDBObject(edgeTwo, true);
          
        // the TextStyle is Currently not on DB
        string sStyle = "ROMANS";

        if (!acTxStyTbl.Has(sStyle))
        {
            acTxStyTbl.UpgradeOpen();
            TextStyleTableRecord acTxStyTblRec = new TextStyleTableRecord();
            acTxStyTblRec.FileName = "romans.shx";
            acTxStyTblRec.Name = sStyle;
            acTxStyTbl.Add(acTxStyTblRec);
            acTrans.AddNewlyCreatedDBObject(acTxStyTblRec, true);

        } 
          
          
        /* Creates a new MText object and assigns it a location,
        text value and text style */
        MText objText = new MText();

        // properties for new Mtext
        objText.Location = offsetLineMiddle;
        objText.Rotation = offsetLineAngle;
        objText.TextHeight = 2.2;
        objText.Layer = "TEXT-2";
        objText.Attachment = AttachmentPoint.BottomCenter;

        // Set the text string for the MText object
        objText.Contents = String.Format("{0:0}", boreDist) + "'" + " BORE";

        // Appends the new MText object to model space
        acBlkTblRec.AppendEntity(objText);

        // Appends to new MText object to the active transaction
        acTrans.AddNewlyCreatedDBObject(objText, true);

        myLine.Erase(true);

        // Saves the changes to the database and closes the transaction
        acTrans.Commit();
      }          
    }

    public Point3d getMidPoint(Line a)
    {


        DBObjectCollection objcoll = a.GetOffsetCurves(1.2);
        Line tempLine = new Line();

        foreach (Entity ent in objcoll)
        {
            tempLine = ((Line)ent);
        }
        
        Point3d point3d  = new Point3d();
        Vector3d vector = new Vector3d();
        vector = tempLine.StartPoint.GetVectorTo(tempLine.EndPoint);
        point3d = tempLine.StartPoint + (vector/2); 
        
        return point3d;
    }


    public void createEdgeLines(Line top, Line bottom)
    {
        

    }

    [CommandMethod("CreateMText")]
    public static void CreateMText()
    {
      // Get the current document and database
      Document acDoc = Application.DocumentManager.MdiActiveDocument;
      Database acCurDb = acDoc.Database;

      // Start a transaction
      using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
      {
        // Open the Block table for read
        BlockTable acBlkTbl;
        acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                      OpenMode.ForRead) as BlockTable;

        // Open the Block table record Model space for write
        BlockTableRecord acBlkTblRec;
        acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                        OpenMode.ForWrite) as BlockTableRecord;

        // Create a multiline text object
        MText acMText = new MText();
        acMText.SetDatabaseDefaults();
        acMText.Location = new Point3d(2, 2, 0);
        acMText.Width = 4;
        acMText.Contents = "This is a text string for the MText object.";

        acBlkTblRec.AppendEntity(acMText);
        acTrans.AddNewlyCreatedDBObject(acMText, true);

        // Save the changes and dispose of the transaction
        acTrans.Commit();
      }
    }


    [CommandMethod("BorA")]
    public void Bora()
    {
      Editor ed = acDoc.Editor;
      //Prompt options for running line
      PromptEntityOptions peo = new PromptEntityOptions("Select Running Line");
      peo.SetRejectMessage("Please Select Running Line");
      peo.AddAllowedClass(typeof(Polyline), false);

      PromptEntityResult perRunningLine = ed.GetEntity(peo);
      if (perRunningLine.Status != PromptStatus.OK)
        return;

      while (checkForDriveways())
      {
        //get the selection set from the prompt results
        //SelectionSet acSSetDriveways = psResultDriveways.Value;

        // Start a transaction
        using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
        {
          //open block table record for read
          BlockTable blkTbl;
          blkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

          //open table record for write
          BlockTableRecord blkTblRec;
          blkTblRec = acTrans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;




          _runningLine = acTrans.GetObject(perRunningLine.ObjectId, OpenMode.ForRead) as Polyline;
          //Polyline drivewayLine = acTrans.GetObject(perDriveways.ObjectId, OpenMode.ForRead) as Polyline;

          //get all point crossing running line
          Point3dCollection ptscoll = getIntersectionPoints();

          // if the points did not intersect the running line return
          if (ptscoll == null) return;



          if (_runningLine != null)
          {
            // we will assume that line only intersect one time for now!!!!! FIX THIS LATER
            bool isOn = isPointOnRunningLine(_runningLine, ptscoll);

            //find the polyline segment using distance at point 
            //getRunningLineSegment()


            //if they intercept then create the offset line and so forth

            ed.WriteMessage(
             "Selected point is {0} on the curve.",
             isOn ? ptscoll[0].ToString() : "not"
           );

            if (isOn)
            {
              
              createPolyline2(ptscoll);
              ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

              //foreach (DBObject dbo in createBoreSymbol(ptscoll))
              //{

              //  //////////////////////blkTblRec.AppendEntity();
              //  acTrans.AddNewlyCreatedDBObject(dbo, true);

              //}

            }

          }
          // Save the changes and dispose of the transaction
          acTrans.Commit();
        }
      }

      return;

    }

    public bool isPointOnRunningLine(Curve cv, Point3dCollection pts)
    {
      //in this function we are going to try an operation if fail then
      //point dont intersect

      foreach (Point3d pt in pts)
      {
        try
        {
          //return true if operation succeeds

          if (pt != null)
          {
            cv.GetDistAtPoint(pt);
            return true;
          }

        }
        catch { break; }
      }

      //otherwise
      return false;

    }

    public Point3dCollection getIntersectionPoints()
    {
      Point3dCollection pts = new Point3dCollection();

      Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

      using (tr)
      {
        //loop through selection 
        foreach (SelectedObject selObj in _ss)
        {
          Type type = tr.GetObject(selObj.ObjectId, OpenMode.ForRead).GetType();

          if (type == typeof(Line) || type == typeof(Polyline) || type == typeof(Curve))
          {
            Polyline intpoly = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Polyline;

            Point3dCollection ptColl = new Point3dCollection();

            _runningLine.IntersectWith(intpoly, Intersect.OnBothOperands, ptColl, IntPtr.Zero, IntPtr.Zero);

            if (ptColl.Count > 0)
            {
              foreach (Point3d pt in ptColl)
              {
                pts.Add(pt);
              }
            }
            else
            {
              Application.ShowAlertDialog("Driveway Line Does not Intersect Runnning Line");
              return null;
            }

          }


        }
      }

      return pts;
    }


    public bool checkForDriveways()
    {

      //Prompt selection options for driveways
      PromptSelectionOptions pso = new PromptSelectionOptions();
      pso.MessageForAdding = "Select Driveways";
      pso.MessageForRemoval = "Remove Line";

      PromptSelectionResult psr = Application.DocumentManager.MdiActiveDocument.Editor.GetSelection(pso);

      if (psr.Status != PromptStatus.OK)
        return false;

      _ss = psr.Value;
      bool flag = false;

      if (_ss.Count == 2)
      {

        Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();

        using (tr)
        {
          foreach (SelectedObject so in _ss)
          {
            Type type = tr.GetObject(so.ObjectId, OpenMode.ForRead).GetType();

            if (type == typeof(Line) || type == typeof(Polyline) || type == typeof(Curve))
            {
              if (flag)
              {
                return true;
              }
              flag = true;
            }
            else break;
          }
        }

      }

      Application.ShowAlertDialog("Must select two driveway lines");
      return false;
    }

    public DBObjectCollection createBoreSymbol(Point3dCollection pts)
    {
      DBObjectCollection dbObjColl = new DBObjectCollection();

      Transaction tr = acCurDb.TransactionManager.StartTransaction();
      using (tr)
      {
        // Open the Layer table for read
        LayerTable acLyrTbl;
        acLyrTbl = tr.GetObject(acCurDb.LayerTableId,
                                    OpenMode.ForRead) as LayerTable;
        LayerTableRecord acLyrRec;
        acLyrRec = tr.GetObject(acCurDb.Clayer,
                                    OpenMode.ForRead) as LayerTableRecord;

        // for mtext bore length
        //Point3d txtInsertion = new Point3d();
        //double txtAngle = 0;

        foreach (Entity ent in _runningLine.GetOffsetCurves(1))
        {
          Polyline offsetTop = (Polyline)ent;
          offsetTop.Layer = acLyrRec.Name;

          //////////txtInsertion = getMidPoint(offsetTop);
          //////////////////////////////////////////////////////i left off here 
          // need to cut the offset line at 1 foot from each side of driveway


          dbObjColl.Add(offsetTop);
        }

        foreach (Entity ent in _runningLine.GetOffsetCurves(-1))
        {
          Polyline offsetBot = (Polyline)ent;
          offsetBot.Layer = acLyrRec.Name;

          dbObjColl.Add(offsetBot);
        }



      }
      return dbObjColl;
    }

    public void createPolyline(Point3dCollection pts)
    {
      
      LineSegment3d segmentLine = null;
      CircularArc3d segmentArc = null;
      int segmentCounter;
      Point3d linePt0 = new Point3d();
      Point3d linePt1 = new Point3d();
      double anglePt0 = -1;
      double anglePt1 = -1;
      bool pt0IsaLine = false;
      bool pt0IsanArc = false;

      CoordinateSystem3d cs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;

      Plane plan = new Plane(Point3d.Origin, cs.Zaxis);

      double param = new double();

      //loop through point and get segment type. Create new lines
      foreach (Point3d pt in pts)
      {
        //oint3d closest = _runningLine.GetClosestPointTo(pt, false);

        for (segmentCounter = 0; segmentCounter < _runningLine.NumberOfVertices; segmentCounter++)
        {
          //try
          //{
          if (_runningLine.OnSegmentAt(segmentCounter, pt.Convert2d(plan), param))
          {
            if (_runningLine.GetSegmentType(segmentCounter) == SegmentType.Arc)
            {
              segmentArc = _runningLine.GetArcSegmentAt(segmentCounter);

              if (pts.IndexOf(pt) == 0)
              {
                anglePt0 = segmentArc.Center.GetVectorTo(pt).AngleOnPlane(plan);
                pt0IsanArc = true;
                break;
              }
                
              if (pt0IsaLine)
              {
                
                if ((segmentLine.GetDistanceTo(pt) / segmentLine.Length) < .5)
                {
                  linePt1 = new Point3d(segmentLine.EndPoint.X, segmentLine.EndPoint.Y, segmentLine.EndPoint.Z);
                }
                else
                {
                  linePt1 = new Point3d(segmentLine.StartPoint.X, segmentLine.StartPoint.Y, segmentLine.StartPoint.Z);
                }

                anglePt0 = segmentArc.Center.GetVectorTo(linePt1).AngleOnPlane(plan);
              }

              anglePt1 = segmentArc.Center.GetVectorTo(pt).AngleOnPlane(plan);
              break;
            }
            else //if not an arc is a line
            {
              segmentLine = _runningLine.GetLineSegmentAt(segmentCounter);

              if (pts.IndexOf(pt) == 0)
              {
                linePt0 = new Point3d(pt.X, pt.Y, pt.Z);
                pt0IsaLine = true;
                break;
              }

              linePt1 = new Point3d(pt.X, pt.Y, pt.Z);

              if (pt0IsanArc)
              {

                if (pt.GetVectorTo(segmentLine.StartPoint).Length > pt.GetVectorTo(segmentLine.EndPoint).Length)
                {
                  linePt0 = new Point3d(segmentLine.EndPoint.X, segmentLine.EndPoint.Y, segmentLine.EndPoint.Z);
                }
                else
                {
                  linePt0 = new Point3d(segmentLine.StartPoint.X, segmentLine.StartPoint.Y, segmentLine.StartPoint.Z);
                }
                anglePt1 = segmentArc.Center.GetVectorTo(linePt0).AngleOnPlane(plan);
              }

              linePt1 = new Point3d(pt.X, pt.Y, pt.Z);
             break;
            }

          }
        }
      }

      Transaction tr = acCurDb.TransactionManager.StartTransaction();
      using (tr)
      {

        Arc dbArc = null;
        Line dbLine = null;
        // Open the Block table record for read
        BlockTable acBlkTbl;
        acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                                    OpenMode.ForRead) as BlockTable;

        // Open the Block table record Model space for write
        BlockTableRecord acBlkTblRec;
        acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                          OpenMode.ForWrite) as BlockTableRecord;

        if (anglePt0 >= 0)
        {
          if (anglePt0 > anglePt1)
          {
            dbArc = new Arc(segmentArc.Center, segmentArc.Radius, anglePt1, anglePt0);
          }
          else
          {
            dbArc = new Arc(segmentArc.Center, segmentArc.Radius, anglePt0, anglePt1);
          }
        }

        if (linePt0 != null)
        {
          dbLine = new Line(linePt0, linePt1);
        }

        if (dbArc != null && dbLine != null)
        {
          Polyline pl1 = new Polyline();
          pl1.AddVertexAt(0, segmentLine.StartPoint.Convert2d(plan), 0, 0, 0);
          pl1.AddVertexAt(1, segmentLine.EndPoint.Convert2d(plan), 0, 0, 0);
          //pl1.AddVertexAt(2, segmentArc.EndPoint.Convert2d(plan), segmentArc.);

          
        }

        if (dbArc != null)
        {
          acBlkTblRec.AppendEntity(dbArc);
          tr.AddNewlyCreatedDBObject(dbArc, true);
        }
 
        if (dbLine != null)
        {
          acBlkTblRec.AppendEntity(dbLine);
          tr.AddNewlyCreatedDBObject(dbLine, true);
        }


        tr.Commit();
      }
    }


    public void createPolyline2(Point3dCollection pts)
    {
      
      CoordinateSystem3d cs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
      Plane plane = new Plane(Point3d.Origin, cs.Zaxis);
      double param = new double();
      Polyline pl = new Polyline();

      bool firstPointSet = false;

      int saveCounter = 0;

      Point3d firstPoint = pts[0];
      Point3d lastPoint = pts[1];

      
      for(int counter = 0; counter < _runningLine.NumberOfVertices; counter++)
      {

        if (_runningLine.OnSegmentAt(counter, firstPoint.Convert2d(plane), param))
        {
          if (_runningLine.GetSegmentType(counter) == SegmentType.Arc)
          {
            pl.AddVertexAt(pl.NumberOfVertices, firstPoint.Convert2d(plane), getBulge(firstPoint, counter), 0, 0); //getBulge(firstPoint, counter)
          }
          else
          {
            pl.AddVertexAt(pl.NumberOfVertices, firstPoint.Convert2d(plane), 0, 0, 0);
          }
          firstPointSet = true;

          if (_runningLine.OnSegmentAt(counter, lastPoint.Convert2d(plane),param))
          {
            saveCounter = counter;
            break;
          }
        }

        else if(firstPointSet)
        {
          if(_runningLine.GetSegmentType(counter) == SegmentType.Arc)
          {
            pl.AddVertexAt(pl.NumberOfVertices, _runningLine.GetPoint2dAt(counter), _runningLine.GetBulgeAt(counter), 0, 0);

          }
          else
          {
            pl.AddVertexAt(pl.NumberOfVertices, _runningLine.GetPoint2dAt(counter), 0, 0, 0);
          }

          if (_runningLine.OnSegmentAt(counter, lastPoint.Convert2d(plane), param))
          {
            saveCounter = counter;
            break;

          }
          
        }

      }
      
      if (_runningLine.GetSegmentType(saveCounter) == SegmentType.Arc)
      {
        pl.AddVertexAt(pl.NumberOfVertices, lastPoint.Convert2d(plane), 0, 0, 0);
        
      }
      else
      {
        pl.AddVertexAt(saveCounter, lastPoint.Convert2d(plane), 0, 0, 0); 
      }
      

      Transaction tr = acCurDb.TransactionManager.StartTransaction();
      using (tr)
      {
        // Open the Block table record for read
        BlockTable acBlkTbl;
        acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                                    OpenMode.ForRead) as BlockTable;

        // Open the Block table record Model space for write
        BlockTableRecord acBlkTblRec;
        acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                          OpenMode.ForWrite) as BlockTableRecord;

        acBlkTblRec.AppendEntity(pl);
        tr.AddNewlyCreatedDBObject(pl, true);
        tr.Commit();
      }
    }

    public double getBulge(Point3d pt, int segNum)
    {

      CoordinateSystem3d cs = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
      Plane plane = new Plane(Point3d.Origin, cs.Zaxis);

      Vector3d vector = _runningLine.GetArcSegmentAt(segNum).Center.GetVectorTo(pt);

      double ang = vector.GetAngleTo(_runningLine.GetArcSegmentAt(segNum).Center.GetVectorTo(_runningLine.StartPoint));
      

      return Math.Tan(ang/4);
    }

  }
}







//  if (_runningLine.GetSegmentType(segmentCounter) == SegmentType.Arc && !pt0IsaLine)
//  {
//    segmentArc = _runningLine.GetArcSegmentAt(segmentCounter);

//    if (pts.IndexOf(pt) == 0)
//    {
//      anglePt0 = segmentArc.Center.GetVectorTo(pt).AngleOnPlane(plan);
//      pt0IsanArc = true;
//      continue;
//    }

//    if (pt0IsaLine)
//    {
//      anglePt1 = segmentArc.StartAngle;
//    }
//    else
//      anglePt1 = segmentArc.Center.GetVectorTo(pt).AngleOnPlane(plan);

//    //Arc dbArc;
//    ////create ad database arc to add to model space
//    //if (anglePt0 > anglePt1)
//    //{
//    //  dbArc = new Arc(segmentArc.Center, segmentArc.Radius, anglePt1, anglePt0);
//    //}
//    //else
//    //{
//    //  dbArc = new Arc(segmentArc.Center, segmentArc.Radius, anglePt0, anglePt1);
//    //}

//    //double oneRadUnit = (dbArc.Length / dbArc.Radius) / (2 * Math.PI);

//    //dbArc.StartAngle = dbArc.StartAngle - oneRadUnit;
//    //dbArc.EndAngle = dbArc.EndAngle + oneRadUnit;

//    ////append to database
//    //acBlkTblRec.AppendEntity(dbArc);
//    //tr.AddNewlyCreatedDBObject(dbArc, true);
//    break;
//  }
//  else if (_runningLine.GetSegmentType(segmentCounter) == SegmentType.Line && !pt0IsanArc)
//  {
//    segmentLine = _runningLine.GetLineSegmentAt(segmentCounter);






//    if (pts.IndexOf(pt) == 0)
//    {
//      linePt0 = pt;
//      pt0IsaLine = true;
//      continue;
//    }

//    if (pt0IsanArc)
//    {
//      linePt0 = segmentLine.StartPoint;
//    }
//    else
//      startPoint = pt;

//    Line dbLine;



//    dbLine = new Line(startPoint, pt);
//    acBlkTblRec.AppendEntity(dbLine);
//    tr.AddNewlyCreatedDBObject(dbLine, true);
//    break;
//  }

//}

//continue;
////}
////catch { break; /*do nothing*/ }





















//05/23/2017

//foreach (Point3d pt in pts)
//      {
        
//        for (int segmentCounter = 0; segmentCounter< _runningLine.NumberOfVertices; segmentCounter++) 
//        { 

//          if (_runningLine.OnSegmentAt(segmentCounter, pt.Convert2d(plane), param))
//          {
//            segments[index] = new Segment(_runningLine.GetSegmentType(segmentCounter).ToString(), segmentCounter);
//            index++;
//          }
//        }
//      }

//      if (segments[0]._verticeNumber == segments[0]._verticeNumber)
//      {
//        int i = 0;
//        foreach (Point3d pt in pts)
//        {
//          if (segments[i]._segType == "1")
//          {
//            pl.AddVertexAt(pl.NumberOfVertices, pt.Convert2d(plane), _runningLine.GetBulgeAt(segments[i]._verticeNumber), 0, 0);
//          }

//          else
//          {
//            pl.AddVertexAt(pl.NumberOfVertices, pt.Convert2d(plane), 0, 0, 0);
//          }
//        }
//      }

//        if (type == SegmentType.Arc)
//        {

          
//          //else
//          //{
//          //  pl.AddVertexAt(pl.NumberOfVertices - 1, _runningLine.GetPoint2dAt(segmentCounter), 0, 0, 0);
//          //}

//        }
//        else
//        {
//          //if (pl.NumberOfVertices == 0 || (pl.NumberOfVertices != 0 && pts.IndexOf(pt) == 1))
//          //{
//          pl.AddVertexAt(pl.NumberOfVertices, pts[0].Convert2d(plane), 0, 0, 0);
//          //else
//          //{
//          //  pl.AddVertexAt(pl.NumberOfVertices - 1, _runningLine.GetPoint2dAt(segmentCounter), 0, 0, 0);

//          //}
//        }
      
        

//        do
//        {


//        }
//        while (_runningLine.NumberOfVertices > segmentCounter);
//                  //else
//                  //{
//                  //  if (type == SegmentType.Arc)
//                  //  {
//                  //    pl.AddVertexAt(pl.NumberOfVertices - 1, _runningLine.GetPoint2dAt(segmentCounter), _runningLine.GetBulgeAt(segmentCounter), 0, 0);
//                  //  }
//                  //  else
//                  //  {
//                  //    pl.AddVertexAt(pl.NumberOfVertices - 1, _runningLine.GetPoint2dAt(segmentCounter), 0, 0, 0);
//                  //  }
//                  //}