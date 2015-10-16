using System.Collections.Generic;
using System.Linq;
using Gnarum.SigmaMeasureFilter.Model.Entities;
using SAP.Middleware.Connector;
using Gnarum.SAP.ConnectionProvider;
using System;
using System.Globalization;



namespace Gnarum.SigmaMeasureFilter.Providers
{
    public interface ISAPProvider
    {
        IList<Meter> GetMeterListByPlant(Plant p);
    }

    public class SAPProvider : ISAPProvider
    {
        // Nombre del destino de SAP a usar (se coge del App.config
        private const string SAP_DESTINATION = "DESARROLLO";

        // Nombre de la RFC de SAP
        private const string RFC_FUNCTION_NAME = "ZW3UFCONTADORES";

        // Nombre de la tabla que devuelve los datos de SAP
        private const string RFC_FUNCTION_RETURN_TABLE_NAME = "T_ZWUFCONTADOR";

        // Campos de la tabla de retorno
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_UF = "ZWUF";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_START_DATE = "ZWBEDAT";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_END_DATE = "ZWENDAT";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_POWER = "ZWPOTCONTA";//"ZWPOTENCIA";//
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_ID = "ZWCONTADOR1";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_PERD = "ZWCPERD1";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER = "ZWPOTENCIA";//"ZWPOTCONTA";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_ID = "ZWCONTADOR2";
        private const string RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_PERD = "ZWCPERD2";

        // Tabla de carga de SAP para pedir datos
        private const string RFC_FUNCTION_TABLE_NAME_LOADER = "T_ZWUFS";
        private const string RFC_FUNCTION_TABLE_NAME_LOADER_FIELD = "LIFNR";

        //Formato de fecha de retorno Sap
        private const string FORMAT_DATE_SAP = "yyyy-MM-dd";


        //public IList<Meter> GetMeterListByPlant(Plant p)
        //{
        //    return new List<Meter>() { new Meter(){ Id = "ABAGI01000", LosingRate = 0, Power = 500 }, new Meter(){ Id = "SSANS01000", LosingRate = 0, Power = 500 } };
        //}

        public IList<Meter> GetMeterListByPlant(Plant p)
        {
            // Plantas de prueba para pedir datos
            //IList<string> idPlantList = new List<string>() { "TRUFW01", "FESFW01", "OLMFW01", "ABECP01" };

            RfcDestination rfcDestination = getRfcDestination();
            IRfcFunction rfcFunction = getRfcFunction(rfcDestination);

            // Preparamos la tabla de carga con los datos que necesitamos
            IRfcTable rfcTable = rfcFunction.GetTable(RFC_FUNCTION_TABLE_NAME_LOADER);
            rfcTable.Append();
            rfcTable.SetValue(RFC_FUNCTION_TABLE_NAME_LOADER_FIELD, p.Id);

            // Hacemos commit de la inserción en SAP
            commitDataInsertion(rfcFunction, rfcDestination);

            // Una vez metidos los datos de entrada, pedimos la tabla de resultados
            IRfcTable returnTable = getReturnTable(rfcDestination, rfcFunction);

            // Conversión a Meter
            IList<Meter> meterList = transformResultDataToMeterList(returnTable);
            //Console.WriteLine(String.Format("Se han encontrado {0} Meters", meterList.Count));

            return meterList;
        }

        /// <summary>
        /// Transforma a Meters el resultado de la tabla
        /// </summary>
        /// <param name="returnTable"></param>
        /// <returns></returns>
        private static IList<Meter> transformResultDataToMeterList(IRfcTable returnTable)
        {
            IList<Meter> result = new List<Meter>();
            foreach (IRfcStructure row in returnTable)
            {
                result = result.Concat(getRowMeterList(row)).ToList();
            }

            return result;
        }

        /// <summary>
        /// Transforma a 1 o 2 Meters (ver caso OLMFW01) una fila de la tabla de resultados
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static IEnumerable<Meter> getRowMeterList(IRfcStructure row)
        {
            IList<Meter> result = new List<Meter>();

            int numberOfCounters = getNumberOfCountersInThisRow(row);

            if (numberOfCounters == 0)
                return result;

            double  num = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER)));
            double den=double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER));
            DateTime startDate = DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_START_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true));
            DateTime endDate = DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_END_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true));
          
            Meter firstMeter = new Meter()
            {
                //IdMeter = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_ID),
                //IdPlant = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_UF),
                //LosingRate = double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_PERD)),
                //Power = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER)) / numberOfCounters),
                //PowerPercentage = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER))
                //    / double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER)))

                IdMeter = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_ID),
                IdPlant = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_UF),
                startDate=DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_START_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true)),
                endDate=DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_END_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true)),
                LosingRate = double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_PERD)),
                Power = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER))),
                PowerPercentage = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER))
                    / double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER)))
            };

            result.Add(firstMeter);

            if (numberOfCounters == 1)
                return result;

            Meter secondMeter = new Meter()
            {
                //IdMeter = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_ID),
                //IdPlant = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_UF),
                //LosingRate = double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_PERD)),
                //Power = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER)) / numberOfCounters),
                //PowerPercentage = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER))
                //    / double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER)))

                IdMeter = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_ID),
                IdPlant = (string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_UF),
                startDate = DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_START_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true)),
                endDate = DateTime.ParseExact((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_END_DATE), FORMAT_DATE_SAP, new CultureInfo("es-ES", true)),
                LosingRate = double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_PERD)),
                Power = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER)) / numberOfCounters),
                PowerPercentage = (double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_POWER))
                    / double.Parse((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_POWER)))
            };

            result.Add(secondMeter);

            return result;
        }

        /// <summary>
        /// Devuelve el numero de Meters distintos en una unica fila
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static int getNumberOfCountersInThisRow(IRfcStructure row)
        {
            int result = 0;
            if (((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT1_ID)).Trim() != string.Empty)
                result++;
            if (((string)row.GetValue(RFC_FUNCTION_RETURN_TABLE_FIELD_CONT2_ID)).Trim() != string.Empty)
                result++;
            return result;
        }

        /// <summary>
        /// Devuelve la función RFC con un nombre determinado y de un destino SAP
        /// </summary>
        /// <param name="rfcDestination"></param>
        /// <returns></returns>
        private static IRfcFunction getRfcFunction(RfcDestination rfcDestination)
        {
            return rfcDestination.Repository.CreateFunction(RFC_FUNCTION_NAME);
        }

        /// <summary>
        /// Devuelve un RfcDestination conectandose a SAP usando el nombre definido y qeu debe aparecer en el App.config
        /// </summary>
        /// <returns></returns>
        private static RfcDestination getRfcDestination()
        {
            return SapConnectionProvider.GetRfcDestination();
        }

        /// <summary>
        /// Realiza commit de los cambios realizados mediante una función RFC
        /// </summary>
        /// <param name="rfcFunction"></param>
        /// <param name="rfcDestination"></param>
        private static void commitDataInsertion(IRfcFunction rfcFunction, RfcDestination rfcDestination)
        {
            RfcTransaction trans = new RfcTransaction();
            trans.AddFunction(rfcFunction);
            trans.Commit(rfcDestination);
        }

        /// <summary>
        /// Invoca a la RFC para que prepare la tabla de retorno
        /// y luego la devuelve
        /// </summary>
        /// <param name="rfcDestination"></param>
        /// <param name="rfcFunction"></param>
        /// <returns></returns>
        private static IRfcTable getReturnTable(RfcDestination rfcDestination, IRfcFunction rfcFunction)
        {
            rfcFunction.Invoke(rfcDestination);
            return rfcFunction.GetTable(RFC_FUNCTION_RETURN_TABLE_NAME);
        }
    }
}
