using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnaliseSBRF
{
    internal class Portfel
    {        
        //Минимальное значение параметра нормировки
        public double minParam;
        //Максимальное значение параметра нормировки
        public double maxParam;
        //Минимальное пороговое значение
        public double minPorog;
        //Максимальное пороговое значение
        public double maxPorog;        
        //Количество разбиений параметра
        public int paramPart = 10;
        //Количество разбиений порога
        public int porogPart = 10;
        //Массив массивов портфелей (виртуальных депозитных счетов)
        public double[][,] deposit;
        

        public static Portfel Inicialize()
        {
            Portfel portfel = new Portfel();
            portfel.Inc(portfel);
            return portfel;
        }

        public static Portfel Inicialize(int maxprt)
        {
            Portfel portfel = new Portfel();
            portfel.paramPart = maxprt;
            portfel.Inc(portfel);
            return portfel;
        }

        public static Portfel Inicialize(int paramPrt, int porogPrt)
        {
            Portfel portfel = new Portfel();
            portfel.paramPart = paramPrt;
            portfel.porogPart = porogPrt;
            portfel.Inc(portfel);
            return portfel;
        }

        private Portfel Inc(Portfel portfel)
        {
            portfel.deposit = new double[10][,];
            for (int i = 0; i < 10; i++)
            {
                portfel.deposit[i] = new double[paramPart, porogPart];
            }

            return portfel;
        }      
    }
}
