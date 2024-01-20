using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geodatabase;

namespace ArcMapClassLibraryVersion2
{
    class DataBaseConnect
    {
        IWorkspace m_pWorkspace;

        public IWorkspace OpenWorkspace(string workspace)
        {
            try
            {
                // Создание экземпляра FileGDBWorkspaceFactory
                IWorkspaceFactory pFileGDBFactory = new FileGDBWorkspaceFactory();

                // Открытие базы данных .gdb из указанного пути
                m_pWorkspace = pFileGDBFactory.OpenFromFile(workspace, 0);
            }
            catch (Exception)
            {
                MessageBox.Show("Ошибка доступа к базе данных");
                return null;
            }

            return m_pWorkspace;
        }

        public IFeatureClass OpenFeatureClass(string name)
        {
            // Проверка, рабочего пространства
            if (m_pWorkspace == null) 
                return null;

            try
            {
                // Открытие класса пространственных объектов по имени
                IFeatureClass featureClass = (m_pWorkspace as IFeatureWorkspace).OpenFeatureClass(name);
                return featureClass;
            }
            catch (Exception)
            {
                // Если произошла ошибка доступа к классу объектов
                MessageBox.Show("Ошибка доступа к классу объектов");
                return null;
            }
        }
    }
}
