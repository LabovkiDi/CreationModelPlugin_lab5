using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin_lab5
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //доступ к документу Revit
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Level level1 = GetLevels(doc, "Уровень 1");
            Level level2 = GetLevels(doc, "Уровень 2");


            //ширина
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            //глубина
            double deepth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            //получение набора точек
            double dx = width / 2;
            double dy = deepth / 2;

            Transaction transaction = new Transaction(doc, "Построение модели");
            transaction.Start();
            {
                //построение стен
                List<Wall> walls = CreateWalls(dx, dy, level1, level2, doc);
                //добавление двери
                AddDoor(doc, level1, walls[0]);
                //добавление окон
                AddWindow(doc, level1, walls[1]);
                AddWindow(doc, level1, walls[2]);
                AddWindow(doc, level1, walls[3]);
            }
            transaction.Commit();

            return Result.Succeeded;
        }

       
        private void AddWindow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0610 x 1220 мм"))   //метод расширения LINQ
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault(); //для получения единичного экземпляра

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0); //левая граница
            XYZ point2 = hostCurve.Curve.GetEndPoint(1); //правая граница
            //на основе 2ух точек, найдем точку куда будем устанавливать дверь
            XYZ point = (point1 + point2) / 2;
            if (!windowType.IsActive)
                windowType.Activate();

            FamilyInstance window = doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            //устанавливаем высоту над уровнем level1 для вставки окна
            //для вставки на заданную высоту используем параметр INSTANCE_SILL_HEIGHT_PARAM-высота нижнего бруса
            double windowHeight = UnitUtils.ConvertToInternalUnits(1200, UnitTypeId.Millimeters);
            window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(windowHeight);
        }

        //метод добавления двери
        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))   //метод расширения LINQ
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault(); //для получения единичного экземпляра

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0); //левая граница
            XYZ point2 = hostCurve.Curve.GetEndPoint(1); //правая граница
            //на основе 2ух точек, найдем точку куда будем устанавливать дверь
            XYZ point = (point1 + point2) / 2;
            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }

        //метод выбора уровня
        public Level GetLevels(Document doc, string levelName)
        {
            //фильтр по уровням
            List<Level> listlevel = new FilteredElementCollector(doc)
                  .OfClass(typeof(Level))
                  .OfType<Level>()
                  .ToList();
            Level level = listlevel
                  .Where(x => x.Name.Equals(levelName))     //фильтр по имени уровня
                 .FirstOrDefault();
            return level;
        }
        //метод создания стен
        public List<Wall> CreateWalls(double dx, double dy, Level level1, Level level2, Document doc)
        {
            //массив, в который добавляем созданные стены
            List<Wall> walls = new List<Wall>();
            //коллекция с точками
            List<XYZ> points = new List<XYZ>();

            //цикл создания стен
            for (int i = 0; i < 4; i++)
            {
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));
                //создание отрезка
                Line line = Line.CreateBound(points[i], points[i + 1]);
                //построение стены по отрезку
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                //находим высоту стены, привязывая ее к уровню
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
                //добавляем в список созданную стену
                walls.Add(wall);
            }
            return walls;
        }
    }
}



