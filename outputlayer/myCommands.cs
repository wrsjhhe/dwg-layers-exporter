// (C) Copyright 2018 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using System.Windows.Forms;

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
            string dir = null;
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                dir = foldPath;
            }

            if (dir == null)
                return;

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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

                    destDb.SaveAs(dir + "\\" + layerName+".dwg", DwgVersion.Current);
                }
            });

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

        static public void TransactionControl(Action handler)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
