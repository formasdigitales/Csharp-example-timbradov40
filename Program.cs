using System;

namespace Timbrado_ejemplov40
{
    class Program
    {
        static void Main(string[] args)
        {   
            //Clase principal el cual hace llamado a la clase en donde se va a procesar y timbrar el xml.
            Procesaxml procesaxml = new Procesaxml();
            procesaxml.loadxml();
        }
    }
}
