# Csharp-example-Timbradov40
Ejemplo de Timbrado CFDI 4.0 con C#

En este ejemplo se muestra como timbrar un cfdi 4.0 con C# a nuestro webservice de pruebas.

La clase principal **Program.cs** es la que se va a encargar de llamar al método **loadxml** la cual hará el procesamiento del xml.

La clase que nos permitira procesar el xml es **Procesaxml.cs** la cual se le asignan variables en donde se cargan el certificado, llaver privada,
el xsl de la cadena original y el xml deseado a timbrar.

##Constructor loadxml()
Permite cargar el xml en un objeto dom para posteriormente hacer el llamada de los metodos **getCurrentTime,loadcertificates,generaCadenaOriginal,generaSello,timbrarXML**.

```C#

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

```

## Métodos de la clase Procesaxml.cs

## getCurrentTime()
Permite recuperar la fecha actual con el formato que solicita el SAT.
```C#
	private String getCurrentTime() {
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    }
```
## loadcertificates()
Permite cargar el archivo .CER para poder extraer el certificado en base64 y el número de certificado llamando al método **getSerialNumber**.
```C#

	private void loadcertificates() {
            X509Certificate2 cert = new X509Certificate2(File.ReadAllBytes(pathcert));

            certificado = Convert.ToBase64String(cert.GetRawCertData());
            numCertificado = getSerialNumber(Encoding.Default.GetString(cert.GetSerialNumber()));
    }

```

## getSerialNumber(String serialnumber)
Extrae el número de certificado.
```C#
	private String getSerialNumber(String serialnumber) {
            String numCertificado = "";
            
            for (int i = 1; i <= serialnumber.Length; i++) {
                String aux = serialnumber.Substring(serialnumber.Length - i, 1);
                numCertificado = numCertificado + aux;
            }
            return numCertificado;
    }

```

##generaCadenaOriginal()
Para poder generar la cadena original es necesario cargar el xslt de la cadena original en un DOM una vez cargado se le pasa el xml para obtener la cadena.
```C#

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

```

##generaSello()
En un arreglo de bytes se carga la llave privada para despues crear un objecto RSA que permitira crear el sello con el algoritmo SHA256 despues se transforma de base64 a String y se le asigna al xml.
```C#
	
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

```

##timbrarXML()
Se crea un objeto de la clase **Accesos** que lleva al **usuario,password** del servicio de formas digitales se crea un objeto **WSTimbradoCFDI** que permitira hacer uso del método **TimbrarCFDIAsync** en donde se le manda el objeto **TimbrarCFDIRequest** que contiene los accesos y el xml en String, si no ocurrio ningún error el webservice regresara el xml timbrado y se plasmará en la consola en caso contrario se visualizará el codigo y el error del por que no se timbro el documento.
```C#
	
	public void timbrarXML() {
            accesos accesos = new accesos();

            accesos.usuario = "pruebasWS";
            accesos.password = "pruebasWS";

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

```