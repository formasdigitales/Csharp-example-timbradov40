using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml.Xsl;
using System.Security;
using System.Security.Cryptography;
using JavaScience;
using Timbrado_Formas;
namespace Timbrado_ejemplov40
{
    class Procesaxml
    {
        XmlDocument doc = new XmlDocument();
        
        String pathxml = "C:/myProjects/C#/Timbrado_ejemplov40/Timbrado_ejemplov40/resource/cfdi_v40_generico.xml"; // Path del xml
        String pathcert = "C:/myProjects/C#/Timbrado_ejemplov40/Timbrado_ejemplov40/resource/ESCUELA_KEMPER_URGATE_EKU9003173C9.cer"; //Path del certificado publico del CSD
        String pathKey = "C:/myProjects/C#/Timbrado_ejemplov40/Timbrado_ejemplov40/resource/ESCUELA_KEMPER_URGATE_EKU9003173C9.key"; // Path de la llave privada del CSD
        String xslcadenoriginalv40 = "C:/myProjects/C#/Timbrado_ejemplov40/Timbrado_ejemplov40/resource/cadenaoriginal_4_0.xslt"; //Path del xslt para poder generar la cadena original


        String cadenaoriginal = "";
        String certificado = "";
        String numCertificado = "";
        String passkey = "12345678a"; // Contraseña del CSD
        
        /*  Metodo donde se carga el xml
            En este llamaremos al metofo getCurrtentTime que nos permite actualizar la fecha del comprobante
            tambien se hace llamado del metodo loadCertificates para cargar al xml los campos del Certificado y NoCertificado,
            generamos la cadena original llamando al metodo generaCadenaOriginal posterior generamos el sello y por ultimo timbramos el xml.
        */
        public void loadxml() {
            doc.Load(pathxml);
            doc.GetElementsByTagName("cfdi:Comprobante").Item(0).Attributes.GetNamedItem("Fecha").Value = getCurrentTime();
            loadcertificates();
            doc.GetElementsByTagName("cfdi:Comprobante").Item(0).Attributes.GetNamedItem("Certificado").Value = certificado;
            doc.GetElementsByTagName("cfdi:Comprobante").Item(0).Attributes.GetNamedItem("NoCertificado").Value = numCertificado;

            generaCadenaOriginal();
            generaSello();
            timbrarXML();
        }
        //Metodo para obtener la fecha actual con formato que pide el SAT para el CFDI
        private String getCurrentTime() {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
        //obtiene los datos del certificado
        private void loadcertificates() {
            X509Certificate2 cert = new X509Certificate2(File.ReadAllBytes(pathcert));

            //cert.Import(pathcert);
            certificado = Convert.ToBase64String(cert.GetRawCertData());
            numCertificado = getSerialNumber(Encoding.Default.GetString(cert.GetSerialNumber()));
        }

        private String getSerialNumber(String serialnumber) {
            String numCertificado = "";
            
            for (int i = 1; i <= serialnumber.Length; i++) {
                String aux = serialnumber.Substring(serialnumber.Length - i, 1);
                numCertificado = numCertificado + aux;
            }
            return numCertificado;
        }

        //Genera la cadena original
        private void generaCadenaOriginal() {
            XmlDocument xsltDoc = new XmlDocument();
            xsltDoc.Load(xslcadenoriginalv40);

            XsltSettings xsltSettings = new XsltSettings(true,true);
            XmlUrlResolver resolver = new XmlUrlResolver();
            XslCompiledTransform myxslTransform = new XslCompiledTransform();
            myxslTransform.Load(xsltDoc, xsltSettings, resolver);

            StringWriter sw = new StringWriter();
            myxslTransform.Transform(doc, null, sw);
            cadenaoriginal = sw.ToString().Replace("\r\n", "");
        }
        //genera el sello despues de generar la cadena original
        private void generaSello() {
            //RSA.Create().ImportPkcs8PrivateKey;

            byte[] keyBytes = File.ReadAllBytes(pathKey);
            SecureString passSecure = new SecureString();
            passSecure.Clear();
            foreach (char c in passkey.ToCharArray()) {
                passSecure.AppendChar(c);
            }
            
            RSACryptoServiceProvider rsa = opensslkey.DecodeEncryptedPrivateKeyInfo(keyBytes,passSecure);
            SHA256 sha256 = SHA256.Create();
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] cadenabytes = utf8.GetBytes(cadenaoriginal);
            byte[] digets = sha256.ComputeHash(cadenabytes);
            RSAPKCS1SignatureFormatter rsaformatter = new RSAPKCS1SignatureFormatter(rsa);
            rsaformatter.SetHashAlgorithm("SHA256");
            byte[] signedhashvalue = rsaformatter.CreateSignature(digets);
            String sello = Convert.ToBase64String(signedhashvalue);
            doc.GetElementsByTagName("cfdi:Comprobante").Item(0).Attributes.GetNamedItem("Sello").Value = sello;

        }
        //Timbra el xml si hay algún error con el xml mostrará en consola el código y mensaje de error en caso contrario se visualizara el xml timbrado.
        public void timbrarXML() {
            accesos accesos = new accesos();

            accesos.usuario = "pruebasWS";
            accesos.password = "pruebasWS";

            WSTimbradoCFDIChannel wSTimbradoCFDIChannel;
            WSTimbradoCFDI wSTimbradoCFDI = new WSTimbradoCFDIClient();
            TimbrarCFDIRequest timbrarCFDIRequest = new TimbrarCFDIRequest();
            timbrarCFDIRequest.accesos = accesos;
            timbrarCFDIRequest.comprobante = doc.OuterXml;

            
            System.Threading.Tasks.Task<TimbrarCFDIResponse> response = wSTimbradoCFDI.TimbrarCFDIAsync(timbrarCFDIRequest);
            acuseCFDI acuse= response.Result.acuseCFDI;
            if (acuse.error != null) {
                Console.WriteLine(acuse.codigoError + " - " + acuse.error);
            }
            else{
                Console.WriteLine(acuse.xmlTimbrado);
            }
            

        }
    }
}
