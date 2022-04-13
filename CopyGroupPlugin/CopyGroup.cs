using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            //получение доступа к документу

            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            //через uiDoc получаем ссылку на экземпляр класса Document, который будет содержать базу данных элементов внутри открытого документа
            Document doc = uiDoc.Document;

            //просим пользователя выбрать группу для копирования

            //ObjectType это перечисление
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");
            //получаем объект типа Element-(родительский класс в RevitAPi)
            Element element = doc.GetElement(reference);
            //чтобы работать с объектом как с группой, мы должны преобразовать объект из базового типа(Element) в тип Group
            Group group = element as Group;

            //запросим у пользователя точку вставки

            XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

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

            //возвращаем результат
            return Result.Succeeded;
        }
    }
}
