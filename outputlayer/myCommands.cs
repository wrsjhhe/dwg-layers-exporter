// (C) Copyright 2018 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;

[assembly: CommandClass(typeof(outputlayer.MyCommands))]

namespace outputlayer
{
    public class MyCommands
    {
        
        HashSet<string> layerNames = new HashSet<string>();
        Dictionary<string, List<Handle>> entitiesMap = new Dictionary<string, List<Handle>>();

        [CommandMethod("Getlayer", CommandFlags.Modal)]
        public void Getlayer() 
        {
            
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = db.TransactionManager;

            List<string> layersName = LayersToList(db);          
           
            TransactionControl(() =>
            {
                foreach (var layerName in layersName)
                {
                    TypedValue[] filterlist = new TypedValue[1];
                    filterlist[0] = new TypedValue(8, layerName);
                    SelectionFilter filter = new SelectionFilter(filterlist);

                    PromptSelectionResult selRes = ed.SelectAll(filter);
                    if (selRes.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage(
                                    "\nerror in getting the selectAll");
                        return;
                    }
                    ObjectId[] ids = selRes.Value.GetObjectIds();
                    ObjectIdCollection sourceIds = new ObjectIdCollection();
                    foreach (var id in ids)
                    {
                        Entity entity = (Entity)tm.GetObject(id, OpenMode.ForRead, true);
                        sourceIds.Add(id);
                    }
                    Database destDb = new Database(true, true);

                    ObjectId sourceMsId = SymbolUtilityServices.GetBlockModelSpaceId(db);

                    ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(destDb);

                    IdMapping mapping = new IdMapping();

                    db.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Replace, false);

                    destDb.SaveAs(@"E:\copied\"+ layerName+".dwg", DwgVersion.Current);
                }



            });
            //SelectAll(null, (ids) =>
            //{
            //    ObjectIdCollection sourceIds = new ObjectIdCollection();
            //    foreach (var id in ids)
            //    {
            //        Entity entity = (Entity)tm.GetObject(id, OpenMode.ForWrite, true);

            //        sourceIds.Add(entity.Id);
            //    }

            //    Database destDb = new Database(true, true);

            //    ObjectId sourceMsId = SymbolUtilityServices.GetBlockModelSpaceId(db);

            //    ObjectId destDbMsId = SymbolUtilityServices.GetBlockModelSpaceId(destDb);

            //    IdMapping mapping = new IdMapping();

            //    db.WblockCloneObjects(sourceIds, destDbMsId, mapping, DuplicateRecordCloning.Replace, false);

            //    destDb.SaveAs(@"E:\copytest\CopyTest.dwg", DwgVersion.Current);

            //});

        }

        public List<string> LayersToList(Database db)
        {
            List<string> lstlay = new List<string>();

            LayerTableRecord layer;
            using (Transaction tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (ObjectId layerId in lt)
                {
                    layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                    lstlay.Add(layer.Name);
                }

            }
            return lstlay;
        }

        static public void SelectAll(string type, Action<ObjectId[]> handler)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptSelectionResult res;

            if (type == null)
            {
                res = ed.SelectAll();
            }
            else
            {
                TypedValue[] value = {
                new TypedValue((int)DxfCode.Start, type)
            };
                SelectionFilter sf = new SelectionFilter(value);
                res = ed.SelectAll(sf);
            }

            SelectionSet SS = res.Value;
            if (SS == null)
                return;

            ObjectId[] idArray = SS.GetObjectIds();

            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = db.TransactionManager;

            TransactionControl(() =>
            {
                handler(idArray);
            });
        }
        static public void TransactionControl(Action handler)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tm = db.TransactionManager;

            doc.LockDocument();
            if (tm.TopTransaction != null)
            {

                handler();
            }
            else
            {
                using (Transaction myT = tm.StartTransaction())
                {
                    handler();
                    try
                    {
                        myT.Commit();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception exception)
                    {
                        ed.WriteMessage(exception.ToString());
                    }
                    finally
                    {
                        myT.Dispose();
                    }
                }
            }
        }

    }

}
