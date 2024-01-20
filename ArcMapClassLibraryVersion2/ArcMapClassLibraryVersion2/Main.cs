using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;

namespace ArcMapClassLibraryVersion2
{
    public partial class Main : Form
    {
        private string filePath; // Переменная для хранения пути к файлу

        public Main()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Добавить файл"
        /// </summary>
        /// <param name="sender">Объект, инициировавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void AddFileButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = folderBrowserDialog.SelectedPath; // Получаем путь к выбранной папке
                textBox2.Text = filePath;

                DataBaseConnect dataBaseConnect = new DataBaseConnect();
                IWorkspace workspace = dataBaseConnect.OpenWorkspace(filePath);

                if (workspace != null)
                {
                    IEnumDataset enumDataset = workspace.get_Datasets(esriDatasetType.esriDTAny);
                    IDataset dataset = enumDataset.Next();

                    while (dataset != null)
                    {
                        if (dataset is IFeatureClass featureClass && featureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                        {
                            checkedListBox1.Items.Add(dataset.Name);
                        }
                        dataset = enumDataset.Next();
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик события нажатия на кнопку "Рассчитать"
        /// </summary>
        /// <param name="sender">Объект, инициировавший событие</param>
        /// <param name="e">Аргументы события</param>
        private void CalculateSumPipelinesButton_Click(object sender, EventArgs e)
        {

            try
            {
                double sumPipelines = 0;

                List<string> selectedFeatureClasses = GetSelectedFeatureClasses(); // Список выбранных классов

                Dictionary<string, double> sumPipelinesByDiameter = new Dictionary<string, double>(); // Словарь ключ - диаметр, значение - сумма линейных объектов для данного ключа

                // Перебор выбранных этажей
                foreach (string featureClassName in selectedFeatureClasses)
                {
                    // Открытие класса пространственных объектов
                    IFeatureClass featureClass = OpenFeatureClass(featureClassName);

                    // Открытие таблицы атрибутов класса пространственных объектов
                    ITable table = (ITable)featureClass;

                    // Добавление поля Length_3D в таблицу атрибутов, если он отсутствует
                    if (table.FindField("Length_3D") == -1)
                    {
                        AddField(table);
                    }

                    // Создаем запрос для получения всех объектов из класса пространственных объектов
                    IQueryFilter queryFilter = new QueryFilterClass();
                    queryFilter.WhereClause = "1=1"; // Получить все объекты

                    // Получаем курсор, содержащий все объекты из класса пространственных объектов
                    IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);

                    // Выполняем расчеты
                    CalculateAndSetLength(featureCursor, table);

                    if (table.FindField("Diameter") == -1)
                    {
                        sumPipelines += CalculateSumLength(featureClassName);
                    }
                    else
                    {
                        // Получение уникальных значений диаметра из таблицы атрибутов
                        CalculateSumByDiameter(table, featureClassName, sumPipelinesByDiameter);
                    }
                }


                foreach (var kvp in sumPipelinesByDiameter)
                {
                    textBox1.Text += "Диаметр " + kvp.Key + "= " + kvp.Value + Environment.NewLine;
                }

                textBox1.Text += "Диаметр отсутствует = " + sumPipelines + Environment.NewLine;
                textBox1.Text += "Таблица атрибутов обновлена!" + Environment.NewLine;

            }
            catch
            {
                MessageBox.Show("Ошибка");
            }

        }

        /// <summary>
        /// Метод для получения выбранных классов пространственных объектов
        /// </summary>
        /// <returns>Список выбранных классов пространственных объектов</returns>
        private List<string> GetSelectedFeatureClasses()
        {
            List<string> selectedFeatureClasses = new List<string>(); // Список для хранения выбранных классов

            // Перебор всех элементов в CheckedListBox1
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    // Добавление выбранного элемента в список
                    selectedFeatureClasses.Add(checkedListBox1.Items[i].ToString());
                }
            }

            return selectedFeatureClasses;
        }

        /// <summary>
        /// Метод для открытия класса пространственных объектов по имени 
        /// </summary>
        /// <param name="featureClassName">Имя класса пространственных объектов, который нужно открыть</param>
        /// <returns>Объект класса пространственных объектов</returns>
        private IFeatureClass OpenFeatureClass(string featureClassName)
        {
            // Создание экземпляра класса DataBaseConnect и открытие рабочего пространства базы данных Access
            DataBaseConnect dataBaseConnect = new DataBaseConnect();
            dataBaseConnect.OpenWorkspace(filePath);

            // Открытие класса пространственных объектов
            IFeatureClass featureClass = dataBaseConnect.OpenFeatureClass(featureClassName);

            return featureClass;
        }

        /// <summary>
        /// Метод для добавления нового поля в таблицу атрибутов
        /// </summary>
        /// <param name="table">Таблица атрибутов, в которую нужно добавить поле</param>
        private void AddField(ITable table)
        {
            // Создание нового поля
            IField newField = new Field();
            IFieldEdit fieldEdit = (IFieldEdit)newField;
            fieldEdit.Name_2 = "Length_3D";
            fieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;

            // Добавление нового поля в таблицу атрибутов
            table.AddField(newField);
        }

        /// <summary>
        /// Метод для вычислений длины линии и заполнение поля, полученным значением
        /// </summary>
        /// <param name="featureCursor">Объект</param>
        /// <param name="table">Таблица атрибутов</param>
        private void CalculateAndSetLength(IFeatureCursor featureCursor, ITable table)
        {
            // Перебираем все объекты
            IFeature feature = featureCursor.NextFeature();

            while (feature != null)
            {
                // Проверяем, является ли объект линией
                if (feature.Shape.GeometryType == esriGeometryType.esriGeometryPolyline)
                {
                    // Получаем геометрию линии
                    IPolyline polyline = (IPolyline)feature.Shape;

                    List<double[]> coordinatesList = GetCoordinates(polyline); // Список координат линии

                    double totalLength = CalculateTotalLength(coordinatesList); // Общая длина линии

                    // Округляем длину до двух цифр после запятой
                    double roundedTotalLength = Math.Round(totalLength, 2);

                    // Получаем индекс поля "Длина линий"
                    int fieldIndex = table.FindField("Length_3D");

                    // Проверяем, что поле было найдено
                    if (fieldIndex != -1)
                    {
                        IRow row = feature; // Получаем строку таблицы для текущей фичи
                        row.set_Value(fieldIndex, roundedTotalLength); // Заполняем поле "Длина линий" значением roundedTotalLength
                        row.Store(); // Сохраняем
                    }

                    feature = featureCursor.NextFeature();
                }
            }
        }

        /// <summary>
        /// Метод для получения координат точек в полилинии
        /// </summary>
        /// <param name="polyline">Объект полилинии, из которого нужно получить координаты точек</param>
        /// <returns>Список координат точек в полилинии</returns>
        private List<double[]> GetCoordinates(IPolyline polyline)
        {
            List<double[]> coordinatesList = new List<double[]>(); // Список координат линии

            // Приведение типов
            IPointCollection pointCollection = polyline as IPointCollection;

            // Перебираем все точки линии
            for (int i = 0; i < pointCollection.PointCount; i++)
            {
                IPoint point = pointCollection.get_Point(i);
                double x = point.X;
                double y = point.Y;
                double z = point.Z;
                double[] coordinates = { x, y, z };
                coordinatesList.Add(coordinates);
            }

            return coordinatesList;
        }

        /// <summary>
        /// Метод для вычисления общей длины линии на основе списка координат
        /// </summary>
        /// <param name="coordinatesList">Список координат линии</param>
        /// <returns>Общая длина линии</returns>
        private double CalculateTotalLength(List<double[]> coordinatesList)
        {
            double totalLength = 0.0; // Общая длина линии

            // Проходим по всем точкам в списке координат
            for (int i = 0; i < coordinatesList.Count - 1; i++)
            {
                double[] point1 = coordinatesList[i]; // Получаем координаты текущей точки
                double[] point2 = coordinatesList[i + 1]; // Получаем координаты следующей точки
                double segmentLength = CalculateSegmentLength(point1, point2); // Вычисляем длину сегмента
                totalLength += segmentLength; // Добавляем длину сегмента к общей длине линии
            }

            return totalLength;
        }

        /// <summary>
        /// Метод для вычисления длины сегмента
        /// </summary>
        /// <param name="point1">Координаты 1 точки</param>
        /// <param name="point2">Координаты 2 точки</param>
        /// <returns>Длина сегмента</returns>
        private double CalculateSegmentLength(double[] point1, double[] point2)
        {
            double deltaX = point2[0] - point1[0]; // Вычисляем разницу между X координатами
            double deltaY = point2[1] - point1[1]; // Вычисляем разницу между Y координатами
            double deltaZ = point2[2] - point1[2]; // Вычисляем разницу между Z координатами

            // Используем теорему Пифагора для вычисления длины сегмента
            double segmentLength = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

            return segmentLength; // Возвращаем вычисленную длину сегмента
        }

        /// <summary>
        /// Метод для очистки фильтров
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            foreach (CheckBox checkBox in Controls.OfType<CheckBox>())
            {
                checkBox.Checked = false;
            }

            textBox1.Text = "";
        }

        private void CalculateSumByDiameter(ITable table, string featureClassName, Dictionary<string, double> sumPipelinesByDiameter)
        {
            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.SubFields = "Diameter";
            queryFilter.WhereClause = "Diameter IS NOT NULL";

            ICursor cursor = table.Search(queryFilter, true);
            IDataStatistics dataStatistics = new DataStatisticsClass();
            dataStatistics.Field = "Diameter";
            dataStatistics.Cursor = cursor;

            IEnumerator enumerator = dataStatistics.UniqueValues;

            while (enumerator.MoveNext())
            {
                string diameter = Convert.ToString(enumerator.Current);

                if (sumPipelinesByDiameter.ContainsKey(diameter))
                {
                    sumPipelinesByDiameter[diameter] += CalculateSumLength(featureClassName, diameter);
                }
                else
                {
                    sumPipelinesByDiameter[diameter] = CalculateSumLength(featureClassName, diameter);
                }
            }
        }

        /// <summary>
        /// Метод для вычисления суммарной длины объекта 
        /// </summary>
        /// <param name="featureClassName">Класс пространственных объектов</param>
        /// <returns>Суммарная длина объекта</returns>
        private double CalculateSumLength(string featureClassName, string diameter = null)
        {
            // Открытие класса пространственных объектов
            IFeatureClass featureClass = OpenFeatureClass(featureClassName);

            // Получение данных из таблицы атрибутов
            ITable table = (ITable)featureClass;
            IFeatureCursor featureCursor = featureClass.Search(null, false);
            IFeature feature = featureCursor.NextFeature();

            double sumLength = 0.0; // Переменная для хранения суммы значений поля Length_3D

            while (feature != null)
            {
                // Получаем значение поля Диаметр для текущей фичи
                int diameterFieldIndex = table.FindField("Diameter");

                if (diameterFieldIndex != -1)
                {
                    string featureDiameter = Convert.ToString(feature.get_Value(diameterFieldIndex));

                    // Проверяем, соответствует ли диаметр текущей фичи указанному диаметру
                    if (featureDiameter == diameter)
                    {
                        // Получаем значение поля Length_3D для текущей фичи
                        int lengthFieldIndex = table.FindField("Length_3D");

                        if (lengthFieldIndex != -1)
                        {
                            double length = Convert.ToDouble(feature.get_Value(lengthFieldIndex));
                            sumLength += length;
                        }
                    }
                }
                else
                {
                    // Получаем значение поля Length_3D для текущей фичи
                    int lengthFieldIndex = table.FindField("Length_3D");

                    if (lengthFieldIndex != -1)
                    {
                        double length = Convert.ToDouble(feature.get_Value(lengthFieldIndex));
                        sumLength += length;
                    }
                }

                feature = featureCursor.NextFeature();
            }

            return sumLength;
        }
    }
}
