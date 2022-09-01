using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Document = Autodesk.AutoCAD.ApplicationServices.Document;
using System;

[assembly: CommandClass(typeof(AutoCADPlugin_helloWorld.MyCommands))]
namespace AutoCADPlugin_helloWorld
{
    public class MyCommands
    {
        [CommandMethod("hello")]
        public void HelloWorld()
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("Hello World");
        }

        [CommandMethod("CheckForPickfirstSelection", CommandFlags.UsePickSet)]
        public static void CheckForPickfirstSelection()
        {
            // Get the current document
            Editor acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;

            // Get the PickFirst selection set
            PromptSelectionResult acSSPrompt;
            acSSPrompt = acDocEd.SelectImplied();

            SelectionSet acSSet;

            // If the prompt status is OK, objects were selected before
            // the command was started
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                acSSet = acSSPrompt.Value;

                Application.ShowAlertDialog("Number of objects in Pickfirst selection: " +
                                            acSSet.Count.ToString());
            }
            else
            {
                Application.ShowAlertDialog("Number of objects in Pickfirst selection: 0");
            }

            // Clear the PickFirst selection set
            ObjectId[] idarrayEmpty = new ObjectId[0];
            acDocEd.SetImpliedSelection(idarrayEmpty);

            // Request for objects to be selected in the drawing area
            acSSPrompt = acDocEd.GetSelection();

            // If the prompt status is OK, objects were selected
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                acSSet = acSSPrompt.Value;

                Application.ShowAlertDialog("Number of objects selected: " +
                                            acSSet.Count.ToString());
            }
            else
            {
                Application.ShowAlertDialog("Number of objects selected: 0");
            }
        }
        [CommandMethod("GettingAttributes")]
        public void GettingAttributes()
        {
            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                ObjectId blkRecId = ObjectId.Null;

                if (!acBlkTbl.Has("TESTBLOCK"))
                {
                    using (BlockTableRecord acBlkTblRec = new BlockTableRecord())
                    {
                        acBlkTblRec.Name = "TESTBLOCK";

                        // Set the insertion point for the block
                        acBlkTblRec.Origin = new Point3d(0, 0, 0);

                        // Add an attribute definition to the block
                        using (AttributeDefinition acAttDef = new AttributeDefinition())
                        {
                            acAttDef.Position = new Point3d(5, 5, 0);
                            acAttDef.Prompt = "Attribute Prompt";
                            acAttDef.Tag = "AttributeTag";
                            acAttDef.TextString = "Attribute Value";
                            acAttDef.Height = 1;
                            acAttDef.Justify = AttachmentPoint.MiddleCenter;
                            acBlkTblRec.AppendEntity(acAttDef);

                            acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForWrite);
                            acBlkTbl.Add(acBlkTblRec);
                            acTrans.AddNewlyCreatedDBObject(acBlkTblRec, true);
                        }

                        blkRecId = acBlkTblRec.Id;
                    }
                }
                else
                {
                    blkRecId = acBlkTbl["CircleBlockWithAttributes"];
                }

                // Create and insert the new block reference
                if (blkRecId != ObjectId.Null)
                {
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(blkRecId, OpenMode.ForRead) as BlockTableRecord;

                    using (BlockReference acBlkRef = new BlockReference(new Point3d(5, 5, 0), acBlkTblRec.Id))
                    {
                        BlockTableRecord acCurSpaceBlkTblRec;
                        acCurSpaceBlkTblRec = acTrans.GetObject(acCurDb.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                        acCurSpaceBlkTblRec.AppendEntity(acBlkRef);
                        acTrans.AddNewlyCreatedDBObject(acBlkRef, true);

                        // Verify block table record has attribute definitions associated with it
                        if (acBlkTblRec.HasAttributeDefinitions)
                        {
                            // Add attributes from the block table record
                            foreach (ObjectId objID in acBlkTblRec)
                            {
                                DBObject dbObj = acTrans.GetObject(objID, OpenMode.ForRead) as DBObject;

                                if (dbObj is AttributeDefinition)
                                {
                                    AttributeDefinition acAtt = dbObj as AttributeDefinition;

                                    if (!acAtt.Constant)
                                    {
                                        using (AttributeReference acAttRef = new AttributeReference())
                                        {
                                            acAttRef.SetAttributeFromBlock(acAtt, acBlkRef.BlockTransform);
                                            acAttRef.Position = acAtt.Position.TransformBy(acBlkRef.BlockTransform);

                                            acAttRef.TextString = acAtt.TextString;

                                            acBlkRef.AttributeCollection.AppendAttribute(acAttRef);
                                            acTrans.AddNewlyCreatedDBObject(acAttRef, true);
                                        }
                                    }
                                }
                            }

                            // Display the tags and values of the attached attributes
                            string strMessage = "";
                            AttributeCollection attCol = acBlkRef.AttributeCollection;

                            foreach (ObjectId objID in attCol)
                            {
                                DBObject dbObj = acTrans.GetObject(objID, OpenMode.ForRead) as DBObject;

                                AttributeReference acAttRef = dbObj as AttributeReference;

                                strMessage = strMessage + "Tag: " + acAttRef.Tag + "\n" +
                                                "Value: " + acAttRef.TextString + "\n";

                                // Change the value of the attribute
                                acAttRef.TextString = "NEW VALUE!";
                            }

                            Application.ShowAlertDialog("The attributes for blockReference " + acBlkRef.Name + " are:\n" + strMessage);

                            strMessage = "";
                            foreach (ObjectId objID in attCol)
                            {
                                DBObject dbObj = acTrans.GetObject(objID, OpenMode.ForRead) as DBObject;

                                AttributeReference acAttRef = dbObj as AttributeReference;

                                strMessage = strMessage + "Tag: " + acAttRef.Tag + "\n" +
                                                "Value: " + acAttRef.TextString + "\n";
                            }

                            Application.ShowAlertDialog("The attributes for blockReference " + acBlkRef.Name + " are:\n" + strMessage);
                        }
                    }
                }

                // Save the new object to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }
    }
}