public EliminarMetadatosService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //Iniciación instancia a File.IO.FileSystemWatcher dada la ruta del directorio que se va a monitorizar
            fsw = new FileSystemWatcher(ruta)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            //Capturamos si FileSystemWatcher reporta un error
            fsw.Error += new ErrorEventHandler(OnError);
            //Capturamos los eventos que suceden en el directorio
            fsw.Created += DirectoryChanged; //Si se añade algún archivo nuevo
            //fsw.Deleted += DirectoryChanged; //Si se elimina algún archivo
            //fsw.Changed += DirectoryChanged; //Si cambia algún archivo existente
            //fsw.Renamed += DirectoryChanged; //Si se renombra algún archivo
            fsw.Filter = "*.*"; //Monitorizará todos los archivos
 
        }

        //Método llamado si se añade un archivo nuevo al directorio monitorizado  
        private void DirectoryChanged(Object sender, FileSystemEventArgs e)
        {
            string msg = DateTime.Now + " - " + e.ChangeType + " - " + e.FullPath + System.Environment.NewLine;


            //Almacenamos donde está el ejecutable una vez instalado
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            if (!ExifToolHasBeenLoaded)
            {
                string command = "\"" + serviceLocation + "\\exiftool.exe\" -stay_open true -@ args.txt -charset filename=utf8";
                //Inicializamos una nueva instancia del proceso ExifTool con los argumentos especificados -stay_open true y -charset
                //filename=utf8 con lo que permanece ExifTool siempre activo y podemos trabajar con archivos cuyo nombre está
                //codificado en UTF8 
                pExifTool.StartInfo = new ProcessStartInfo("cmd", String.Format("/c \"{0}\"", @command));

                pExifTool.StartInfo.RedirectStandardOutput = true;
                pExifTool.StartInfo.RedirectStandardError = true;
                pExifTool.ErrorDataReceived += new DataReceivedEventHandler(ETErrorHandler); 
                pExifTool.OutputDataReceived += new DataReceivedEventHandler(ETDataHandler);

                pExifTool.StartInfo.UseShellExecute = false;
                pExifTool.StartInfo.CreateNoWindow = true;
                pExifTool.Start();  //Iniciamos Exiftool
                pExifTool.BeginErrorReadLine();  //Capturamos si ExifTool reporta un error
                pExifTool.BeginOutputReadLine();
                ExifToolHasBeenLoaded = true;

            }

            //Para que discrimine los ficheros temporales (terminados en exiftool_tmp) que genera ExifTool en el mismo directorio 	     
            //donde se eliminan los metadatos, lo que provoca error al intentar eliminar los metadatos de éstos ficheros temporales 	     
            //que se elimina automáticamente pero el FileSystemWatcher los detecta 
            if (!e.FullPath.Contains("_exiftool_tmp"))
            {
		    //Añade al fichero args.txt los argumentos junto con la ruta completa a ejecutar con ExifTool (línea por línea el 		   
            //fichero queda como en el ejemplo siguiente (sin la doble barra de comentario):
		   	//-all =
		   	//C:\Program Files (x86)\Eliminar Metadatos\Eliminar Metadatos\Eliminar Metadatos\Parque.jpg
		   	//-overwrite_original
		   	//-charset
		   	//filename=utf8
		   	//-execute
                string[] args = new string[] { "-all =", e.FullPath, "-overwrite_original", "-charset", "filename=utf8", "-execute" };
                File.AppendAllLines(serviceLocation + "\\args.txt", args);
            }
        }

        //Método llamado cuando FileSystemWatcher detecta un error, lo muestra en el EventLog.
        private void OnError(object source, ErrorEventArgs e)
        {
            EventLog.WriteEntry("FileSystemWatcher ha detectado un error");
            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                EventLog.WriteEntry(("FileSystemWatcher ha experimentado un buffer overflow: " + e.GetException().Message));
            }
        }

        //Manejador de errores asíncrono para ExifTool, muestra los errores en el EventLog
        private void ETErrorHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                EventLog.WriteEntry("Error en ExifTool, " + errLine.Data);
            }
        }

        //Muestra los datos de salida de ExifTool en el EventLog
        private void ETDataHandler(object sendingProcess, DataReceivedEventArgs dataLine)
        {
            if (!String.IsNullOrEmpty(dataLine.Data))
            {
                EventLog.WriteEntry("Datos de salida de ExifTool: " + dataLine.Data);
            }
        }

