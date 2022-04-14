using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{

    //TransactionMode.Manual-обозначает ручной режим.Мы сами решаем в какой момент транзакция должна открыться, в какой завершиться
    [TransactionAttribute(TransactionMode.Manual)]

    public class CopyGroup : IExternalCommand
    {
        //метод Execute должен возращать значение Result, который указывает успешно или нет завершилась команда

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //введем блок try catch для обработки исключений
            try
            {


            //получение доступа к документу

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            //через uiDoc получаем ссылку на экземпляр класса Document, который будет содержать базу данных элементов внутри открытого документа
            Document doc = uiDoc.Document;

                //сощдаем экземпляр класса GroupPickFilter и передаем его вторым аргументов в метод PickObject
                GroupPickFilter groupPickFilter = new GroupPickFilter();

            //просим пользователя выбрать группу для копирования

            //ObjectType это перечисление
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
            //получаем объект типа Element-(родительский класс в RevitAPi)
            Element element = doc.GetElement(reference);
            //чтобы работать с объектом как с группой, мы должны преобразовать объект из базового типа(Element) в тип Group
            Group group = element as Group;
                //нахождения центра группы
                XYZ groupCenter = GetElementCenter(group);
                //определим комнату в которой находится выбранная исходная группа объектов
                Room room = GetRoomByPoint(doc, groupCenter);
                //находим центр этой комнаты
                XYZ roomCenter = GetElementCenter(room);
                //определение смещения центра группы объектов относительно центра комнаты
                XYZ offset = groupCenter - roomCenter;

                //запросим у пользователя точку вставки

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");
                //определяем какой комнате принадлежит эта точка
                Room selectedRoom = GetRoomByPoint(doc, point);
                //определяем центр выбранной комнаты
                XYZ selectedRoomCenter = GetElementCenter(selectedRoom);
                //определяем точку вставки группы объектов в выбранную комнату на основе центра выбранной комнаты и найденного смещения
                XYZ insertPoint = offset + selectedRoomCenter;

                //вставка группы в указанную точку

                //т.к. вставка группы объектов приведет к изменению модели, необходимо воспользоваться транзакцией
                Transaction transaction = new Transaction(doc);
            //начало транзакции
            transaction.Start("Копирование группы объектов");
            //1.обращаемся к нашему документу(базе данных модели)2. затем к его свойству Create 
            //3.вызываем метод PlaceGroup, в качестве аргумента передаем точку point и второй аргумент group.GroupType
            doc.Create.PlaceGroup(point, group.GroupType);
            //конец транзакции
            transaction.Commit();
            }
            //обработка исключений, связанной с нажатием отмены/Ecs
            catch(Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                //прекращаем работу плагина, возвращая результат отмена
                return Result.Cancelled;
            }
            catch(Exception ex)
            {
                message = ex.Message; //сообщение об ошибке, вызванной исключением
                //завершается работа приложения, с результатом ошибки
                return Result.Failed;
            }
            //возвращаем результат
            return Result.Succeeded;
        }
        //метод, вычисляющий по объекту центр, но основе BoundingBox
        public XYZ GetElementCenter(Element element)
        {
            //это рамка в трех измерениях, т.е. п/у параллелипипед
           BoundingBoxXYZ bounding= element.get_BoundingBox(null);
            //нахождение центра
            return (bounding.Max + bounding.Min) / 2;
        }
        //метод определяющий комнату по исходной точке
        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            //для нахождения нужного нам элемента воспользуемся FilteredElementCollector
            //аргументом данного конструктора является ссылка на doc документ, где проводится поиск
            FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfCategory(BuiltInCategory.OST_Rooms);
            //переберем все содержимое фильтра
            foreach(Element e in collector)
            {
                Room room = e as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }
    //создаем класс для фильтра
    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            //если элемент имеет категорию id выраженное числом такое же как OSt_iosModelGroups тогда возвращаем истину
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
