// Cristina Muñoz Muñoz
// Aroa Yubero Sevilla
using System;
using System.IO;
using System.Text;
using System.Media;

namespace Practica1
{
    class Program
    {
        static Random rnd = new Random(); // un único generador de aleaotorios para todo el programa
        const bool DEBUG = false; // para sacar información adicional en el Render
        // área de juego
        const int ANCHO = 27,
                  ALTO = 15,
                  MAX_BALAS = 5,
                  MAX_ENEMIGOS = 9, 
                  COLISIONES = MAX_BALAS+1;
        struct Tunel
        {
            public int[] suelo, techo;
            public int ini;
        }
        struct Entidad
        {
            public int fil, col;
        }
        struct GrEntidades
        {
            public Entidad[] ent;
            public int num;
        }
        static void Main()
        {
            IniciaTunel(out Tunel tunel); //iniciar tunel
                                         
            Entidad nave = new Entidad(); //inicializar nave
            //inicializacion posicion nave centrada respecto al suelo y al techo en dicha columna
            int aux = ANCHO / 2;
            int t = tunel.techo[aux];
            int s = tunel.suelo[aux];
            int pos = (t + s) / 2;
            nave.fil = pos;
            nave.col = ANCHO / 2;

            InicializaArray(out GrEntidades enemigos, MAX_ENEMIGOS); //creo array enemigos
            InicializaArray(out GrEntidades balas, MAX_BALAS);       //creo array balas
            InicializaArray(out GrEntidades colisiones, COLISIONES); //creo array colisiones

            //preguntar si quiere leer de archivo una partida 
            int inicializar;
            Console.Write("¿Desea cargar la partida guardada (pulse 1) o comenzar partida nueva (pulse otro número)? ");
            inicializar = int.Parse(Console.ReadLine());
            if (inicializar == 1) //Si se lee de archivo se "sustituyen" el tunel, la posicion de la nave, los enemigos y las balas. Si no, se inicializa el juego de cero.
            {
                LeeArchivo(ref  tunel, ref  nave,ref enemigos, ref balas);
            }
            Console.Clear();

            //musica de fondo
            SoundPlayer music = new SoundPlayer("musica espacial. planetas y cometas.wav");
            music.Load();
            music.PlayLooping();

            bool colision = false; //booleano para determinar si ha habido colision de la nave con tunel o enemigos
            bool abortar = false;  //booleano para saber si el jugador quiere abortar la partida

            while (!colision && !abortar)
            {
                AvanzaTunel(ref tunel);
                GeneraEnemigos(ref enemigos, tunel);
                AvanzaEnemigos(ref enemigos);
                Colisiones(tunel, ref nave, ref balas, ref enemigos, ref colisiones);
                if (nave.fil == -1) colision = true; //si la nave ha colisionado con enemigo o tunel la partida acaba
                if (colision == false)//si no ha colisionado...
                {
                    char ch = LeeInput();
                    if (ch != ' ')
                    {
                        AvanzaNave(ch, ref nave);
                        if (ch == 'x') GeneraBala(ref balas, nave); ;
                        if (ch == 'q')
                        {
                            abortar = true; //si se pulsa 'q' se aborta la partida}
                            music.Stop();   //se para la musica
                        }
                    }
                    AvanzaBalas(ref balas);
                    Colisiones(tunel, ref nave, ref balas, ref enemigos, ref colisiones);
                }
                Render(tunel, nave, enemigos, balas, colisiones);
                Thread.Sleep(100);
                colisiones.num = 0; //primero se muestran las colisiones en pantalla pero solo durante un frame, por lo tanto en cada frame se vuelven a poner a 0

                if (abortar)
                {
                    Console.CursorTop = ALTO;
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = ConsoleColor.White;
                    int pregunta;
                    Console.Write("¿Desea guardar la partida (pulse 1) ? Si no, pulsa otro número. "); //preguntar si se quiere guardar la partida
                    pregunta = int.Parse(Console.ReadLine());
                    if (pregunta == 1) //si se pulsa 1, la partida se guarda 
                    {
                        GuardaPartida(tunel, nave, ref enemigos, ref balas);
                        Console.WriteLine("GUARDADO");
                    }
                    else Console.WriteLine("FIN PARTIDA");
                }
                if (colision)
                {
                    music.Stop(); //se para la musica
                    Console.CursorTop = ALTO;
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("GAME OVER");
                }
            }
            Console.ReadKey();
        }
        static void InicializaArray(out GrEntidades array, int n) //metodo para crear los respectivos arrays de enemigos,balas y colisiones
        {
            array.num = 0;              //se inicializa .num a 0
            array.ent = new Entidad[n]; //se crea el array con el MAX respectivo de GrEntidades
        }
        static void IniciaTunel(out Tunel tunel)
        {
            // creamos arrays
            tunel.suelo = new int[ANCHO];
            tunel.techo = new int[ANCHO];

            // rellenamos posicion 0 como semilla para generar el resto
            tunel.techo[0] = 0;
            tunel.suelo[0] = ALTO - 1;

            // dejamos 0 como la última y avanzamos hasta dar la vuelta
            tunel.ini = 1;
            for (int i = 1; i < ANCHO; i++)
            {
                AvanzaTunel(ref tunel);
            }
            // al dar la vuelta y quedará tunel.ini=0    
        }
        static void AvanzaTunel(ref Tunel tunel)
        {
            // ultima pos del tunel: anterior a ini de manera circular
            int ult = (tunel.ini + ANCHO - 1) % ANCHO;

            // valores de suelo y techo en la última posicion
            int s = tunel.suelo[ult],
                t = tunel.techo[ult]; // incremento/decremento de suelo/techo

            // generamos nueva columna a partir de esta última
            int opt = rnd.Next(5); // obtenemos un entero de [0,4]
            if (opt == 0 && s < ALTO - 1) { s++; t++; }   // tunel baja y mantiene ancho
            else if (opt == 1 && t > 0) { s--; t--; }   // sube y mantiene ancho
            else if (opt == 2 && s - t > 7) { s--; t++; } // se estrecha (como mucho a 5)
            else if (opt == 3)
            {                    // se ensancha, si puede
                if (s < ALTO - 1) s++;
                if (t > 0) t--;
            } // con 4 sigue igual

            // guardamos nueva columna del tunel generada
            tunel.suelo[tunel.ini] = s;
            tunel.techo[tunel.ini] = t;

            // avanzamos la tunel.ini: siguiente en el array circular
            tunel.ini = (tunel.ini + 1) % ANCHO;
        }
        static void RenderTunel(Tunel tunel)
        {
            //se coloca el cursor arriba a la izquierda
            Console.CursorTop = 0;
            Console.CursorLeft = 0;
            Console.CursorVisible = false;

            int i = tunel.ini; //variable auxiliar que comienza en tunel.ini
            for(int fila=0; fila<ALTO; fila++) //se dibuja en pantalla el tunel por filas y columnas
            { 
                for (int col = 0; col < ANCHO; col++)
                {
                    if (tunel.techo[i] >= fila || tunel.suelo[i] <= fila) //si el techo es mayor o igual o el suelo menor o igual que el numero de fila, es tunel y se pinta de azul
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.Write("  ");
                    }
                    else                                                  //en caso contrario no es tunel y se pinta de negro
                    {
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.Write("  ");
                    }
                    i = (i + 1) % ANCHO; //siguiente "columna"
                }
                Console.WriteLine();//cuando se terminan de recorrer todas las columnas, se salta de linea y se pasa a la siguiente fila
            }
        }
        static void AñadeEntidad(Entidad ent, ref GrEntidades gr)
        {
           if(gr.num < gr.ent.Length) //si no se ha llegado al MAX del array del respectivo GrEntidades
           {
                gr.ent[gr.num] = ent; //añadir la entidad ent al respectivo array en .num (ultima posicion libre del array)
                gr.num++;             //incrementar .num 
           }
        }
        static void EliminaEntidad(int i, ref GrEntidades gr)
        {
            gr.ent[i] = gr.ent[gr.num - 1]; //se intercambia la posicion de la entidad que se quiere eliminar por la ultima
            gr.num--;                       //se decrementa .num y asi la ultima entidad (que es la que se quiere eliminar,intercambiada ya) "se queda fuera del array"
        }
        static void AvanzaNave(char ch, ref Entidad nave)
        {
            if (ch == 'l' && nave.col > 0) nave.col--;              //si se pulsa 'l' y no se sale del area de juego por la izquierda, se decrementa una columna
            else if (ch == 'r' && nave.col < ANCHO - 1) nave.col++; //si se pulsa 'r' y no se sale del area de juego por la derecha, se incrementa una columna
            else if (ch == 'u') nave.fil--;                         //si se pulsa 'u' se decrementa una fila
            else if (ch == 'd') nave.fil++;                         //si se pulsa 'd' se incrementa una fila
        }
        static void Render(Tunel tunel, Entidad nave,  GrEntidades enemigos, GrEntidades balas,  GrEntidades colisiones)
        { 
           RenderTunel(tunel); //renderizado del tunel
           if(nave.fil>-1)     //si la nave no ha colisionado, es decir, se encuentra en el area de juego
           {
                Console.SetCursorPosition(nave.col * 2, nave.fil); //se posiciona el cursor en la respectiva fila y columna de la nave
                Console.ForegroundColor = ConsoleColor.Green;      //se pinta la nave de verde
                Console.BackgroundColor = ConsoleColor.Black;      //se pinta el fondo de negro
                Console.Write("=>");//nave
               
           }
           for (int i=0; i<enemigos.num; i++)
           {
                Console.SetCursorPosition(enemigos.ent[i].col * 2, enemigos.ent[i].fil); //se posiciona el cursor en las respectivas filas y columnas de los enemigos
                Console.ForegroundColor = ConsoleColor.DarkYellow;                       //se pintan los enemigos de amarillo
                Console.BackgroundColor = ConsoleColor.Black;                            //se pinta el fondo de negro
                Console.Write("<>");//enemigos
           }
           for(int j=0; j<balas.num; j++)
           {
                Console.SetCursorPosition(balas.ent[j].col * 2, balas.ent[j].fil); //se posiciona el cursor en la respectivas filas y columnas de las balas
                Console.ForegroundColor = ConsoleColor.Magenta;                    //se pintan las balas de magenta
                Console.BackgroundColor = ConsoleColor.Black;                      //se pinta el fondo de negro
                Console.Write("--");//balas
           }
          
           for (int k = 0; k < colisiones.num; k++)
           {
                Console.SetCursorPosition(colisiones.ent[k].col * 2, colisiones.ent[k].fil); //se posiciona el cursor en la respectivas filas y columnas de las colisiones
                Console.ForegroundColor = ConsoleColor.Red;                                  //se pintan las colisiones de rojo
                Console.BackgroundColor = ConsoleColor.Black;                                //se pinta el fondo de negro
                Console.Write("**");//colisiones
           }
        }
        static void GeneraEnemigos(ref GrEntidades enemigos, Tunel tunel)
        {
            if (enemigos.num < MAX_ENEMIGOS) //se pueden generan enemigos hasta no alcanzar el máximo numero de enemigos establecido al principio
            {
                int caso = rnd.Next(0,4); //probabilidad del 25% para generar enemigos, se genera un numero aleatorio entre 4
                if(caso==0)
                {
                    int ult = (tunel.ini + ANCHO - 1) % ANCHO; //la ultima columna del tunel
                    int t = tunel.techo[ult];                  //se toma el techo de la ultima columna
                    int s = tunel.suelo[ult];                  //se toma el suelo de la ultima columna
                    int pos = rnd.Next(t+1, s);                //se toma una posicion aleatoria entre el techo y suelo de la ultima columna
                    enemigos.ent[enemigos.num].fil = pos;      //los enemigos se situan en una fila aleatoria entre el techo y suelo de la ultima columna
                    enemigos.ent[enemigos.num].col = ANCHO;     //los enemigos aparecen por la derecha del area del juego
                    AñadeEntidad(enemigos.ent[enemigos.num], ref enemigos); //como se ha generado un enemigo, se añade la entidad generada al array enemigos
                }
            }
        }
        static void AvanzaEnemigos(ref GrEntidades enemigos)
        {
            int i = 0;
            while (i < enemigos.num)           //para cada uno de los enemigos 
            {
                enemigos.ent[i].col--;         //se decrementa una columna (se desplaza hacia la izquierda)
                if (enemigos.ent[i].col == -1) //si el enemigo ha salido del area de juego por la izquierda
                {
                    EliminaEntidad(i, ref enemigos); //se elimina ese enemigo del array enemigos
                }
               else i++;
            }
        }
        static void GeneraBala(ref GrEntidades balas, Entidad nave)
        {
            //se pueden generan balas hasta no alcanzar el máximo numero de balas establecido al principio y si la nave no se encuentra en el extremo derecho del area del juego
            if (balas.num < MAX_BALAS  && nave.col<ANCHO-1)
            {
                //se genera una bala justo en la posicion de la nave
                balas.ent[balas.num].col = nave.col;
                balas.ent[balas.num].fil = nave.fil;
                AñadeEntidad(balas.ent[balas.num], ref balas); //como se ha generado una bala, se añade la entidad generada al array balas
            }
        }
        static void AvanzaBalas(ref GrEntidades balas)
        {
            int i = 0;
            while (i < balas.num)              //para cada una de las balas
            {
                balas.ent[i].col++;            //se incrementa una columna (se desplaza hacia la derecha)
                if (balas.ent[i].col == ANCHO) //si la bala ha salido del area de juego por la derecha
                {
                    EliminaEntidad(i, ref balas); //se elimina esa bala del arra balas
                }
                else i++;
            }
        }
        static void ColNaveTunel(Tunel tunel, ref Entidad nave, ref GrEntidades colisiones) 
        {
            int ind = (nave.col + tunel.ini)%ANCHO;            //indice para saber donde se encuentra la nave respecto al tunel

            if (nave.fil > -1 && nave.fil <= tunel.techo[ind]) //si la nave se encuentra en una fila igual o menor a techo
            {
                    AñadeEntidad(nave, ref colisiones);        //se añade colision al array colisiones en la posicion donde han colisionado nave y tunel
                    nave.fil = -1;                             //se "elimina" la nave variando su posicion fuera del area del juego
                    tunel.techo[ind]--;                        //se "destruye" el techo del tunel
            }
            else if (nave.fil > -1 && nave.fil >= tunel.suelo[ind]) //si la nave se encuentra en una fila igual o mayor a suelo
            {
                    AñadeEntidad(nave, ref colisiones);        //se añade colision al array colisiones en la posicion donde han colisionado nave y tunel
                    nave.fil = -1;                             //se "elimina" la nave variando su posicion fuera del area del juego
                    tunel.suelo[ind]++;                        //se "destruye" el suelo del tunel
            }
        }
        static void ColBalasTunel(Tunel tunel, ref GrEntidades balas, ref GrEntidades colisiones)
        {
            int ba = 0; //variable auxiliar balas
            while(ba<balas.num)
            {
                int ind = (balas.ent[ba].col + tunel.ini) % ANCHO; //indice para saber donde se encuentran las balas respecto al tunel
                if (balas.ent[ba].fil <= tunel.techo[ind])         //si la bala se encuentra en una fila igual o menor a techo
                {
                    AñadeEntidad(balas.ent[ba], ref colisiones);   //se añade colision al array colsiones en la posicion donde han colisionado bala y tunel
                    EliminaEntidad(ba, ref balas);                 //se elimina la respectiva bala del array balas
                    for(int t = tunel.techo[ind];t> balas.ent[ba].fil-1; t--) //se "destruye" el techo del tunel hasta la fila donde se ha producido la colision
                    {
                        tunel.techo[ind]--;
                    }
                }
                else if(balas.ent[ba].fil >= tunel.suelo[ind])     //si la bala se encuentra en una fila igual o mayor a suelo
                {
                    AñadeEntidad(balas.ent[ba], ref colisiones);   //se añade colision al array colsiones en la posicion donde han colisionado bala y tunel
                    EliminaEntidad(ba, ref balas);                 //se elimina la respectiva bala del array balas
                    tunel.suelo[ind]++;                            //se "destruye" el suelo
                }
                else ba++;
            }
        }
        static void ColNaveEnemigos(ref Entidad nave,ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            int i = 0;
            while (i < enemigos.num)
            {
                
                if (nave.col == enemigos.ent[i].col && nave.fil == enemigos.ent[i].fil) //si la posicion de la nave coincide con la posicion del enemigo
                {
                    EliminaEntidad(i, ref enemigos);    //se elimina el enemigo del array enemigos
                    AñadeEntidad(nave, ref colisiones); //se añade una colision al array colisiones en la posicion donde han colisionado nave y enemigo
                    nave.fil = -1;                      //se "elimina" la nave variando su posicion fuera del area del juego
                }
                else i++;
            }
        }
        static void ColBalasEnemigos(ref GrEntidades balas, ref GrEntidades enemigos, ref GrEntidades colisiones)
        {
            int ba = 0,en=0; //variables auxiliares de balas y enemigos
            while (ba < balas.num)
            {
                while(en<enemigos.num)
                {
                    if (enemigos.ent[en].col == balas.ent[ba].col && enemigos.ent[en].fil == balas.ent[ba].fil) //si un enemigo se encuentra en la misma posicion (fil y col) que una bala
                    {
                        AñadeEntidad(enemigos.ent[en], ref colisiones); //se añade una colision al array colisiones en la posicion donde han colisionado bala y enemigo
                        EliminaEntidad(ba, ref balas);                  //se elimina la respectiva bala del array balas
                        EliminaEntidad(en, ref enemigos);               //se elimina el respectivo enemigo del array enemigos
                    }
                    else en++;
                }
                ba++;
            }
        }
        static void Colisiones(Tunel tunel, ref Entidad nave,ref GrEntidades balas,ref  GrEntidades enemigos, ref GrEntidades colisiones)
        {
           //metodo que llama a todos los metodos anteriores de colisiones
           ColNaveTunel(tunel, ref nave, ref colisiones);
           ColBalasTunel(tunel, ref balas, ref colisiones);
           ColNaveEnemigos(nave, ref enemigos, ref colisiones);
           ColBalasEnemigos(balas, ref enemigos, ref colisiones);
        }
        static void GuardaPartida(Tunel tunel, Entidad nave, ref GrEntidades enemigos, ref GrEntidades balas)
        {
            StreamWriter guarda = new StreamWriter("saved.txt");
            for (int i = 0; i < tunel.techo.Length; i++) //se guarda el techo del tunel en una linea del archivo de texto
            {
                guarda.Write(tunel.techo[i] + " ");
            }
            guarda.WriteLine();
            for (int i = 0; i < tunel.suelo.Length; i++) //se guarda el suelo del tunel en una linea del archivo de texto
            {
                guarda.Write(tunel.suelo[i] + " ");
            }
            guarda.WriteLine();
            guarda.WriteLine(tunel.ini); //se guarda tunel.ini en una linea del archivo de texto
            guarda.WriteLine(nave.fil);  //se guarda la fila de la nave en una linea del archivo de texto
            guarda.WriteLine(nave.col);  //se guarda la columna de la nave en una linea del archivo de texto
            guarda.WriteLine(enemigos.num); //se guarda el numero de enemigos en una linea del archivo de texto
            for (int i=0; i<enemigos.num; i++) //se guardan las columnas de cada uno de los enemigos en una linea del archivo de texto
            {
                guarda.Write(enemigos.ent[i].col + " ");
            }
            guarda.WriteLine();
            for (int i = 0; i < enemigos.num; i++) //se guardan las filas de cada uno de los enemigos en una linea del archivo de texto
            {
                guarda.Write(enemigos.ent[i].fil + " ");
            }
            guarda.WriteLine();
            guarda.WriteLine(balas.num); //se guarda el numero de enemigos en una linea del archivo de texto
            for (int i = 0; i < balas.num; i++) //se guardan las columnas de cada una de las balas en una linea del archivo de texto
            {
                guarda.Write(balas.ent[i].col + " ");
            }
            guarda.WriteLine();
            for (int i = 0; i < balas.num; i++) //se guardan las filas de cada una de las balas en una linea del archivo de texto
            {
                guarda.Write(balas.ent[i].fil + " ");
            }
            guarda.Close();
        }
        static void LeeArchivo(ref Tunel tunel, ref Entidad nave,ref GrEntidades enemigos, ref GrEntidades balas)
        {
            StreamReader lee = new StreamReader("saved.txt");
            //para todos los arrays que se van a leer primero se lee la linea entera en un string y despues se "trocea" este string y se guarda cada valor en un array de string
            //despues se convierte el string a int

            string a = new string(lee.ReadLine());
            string[] b =a.Split(' ');
            for(int i = 0; i < b.Length - 1; i++) //se lee el techo del tunel
            {
                tunel.techo[i] = int.Parse(b[i]);
            }
            string c = new string(lee.ReadLine());
            string[] d = c.Split(' ');
            for (int i = 0; i < d.Length - 1; i++) //se lee el suelo del tunel
            {
                tunel.suelo[i] = int.Parse(d[i]);
            }
            tunel.ini = int.Parse(lee.ReadLine()); //se lee tunel.ini
            nave.fil = int.Parse(lee.ReadLine());  //se lee la fila de la nave
            nave.col = int.Parse(lee.ReadLine());  //se lee la columna de la nave
            enemigos.num=int.Parse(lee.ReadLine()); //se lee el numero de enemigos
            string g = new string(lee.ReadLine());
            string[] h = g.Split(' ');
            for( int i = 0; i < enemigos.num; i++) //se leen las columnas de cada uno de los enemigos
            {
                enemigos.ent[i].col = int.Parse(h[i]); 
            }
            string j = new string(lee.ReadLine());
            string[]k = j.Split(' ');
            for (int i = 0; i < enemigos.num; i++) //se leen las filas de cada uno de los enemigos
            {
                enemigos.ent[i].fil = int.Parse(k[i]);
            }
            balas.num=int.Parse(lee.ReadLine());  //se lee el numero de balas
            string l = new string(lee.ReadLine());
            string[] m = l.Split(' ');
            for(int i=0; i < balas.num; i++) //se leen las columnas de cada una de las balas
            {
                balas.ent[i].col = int.Parse(m[i]);
            }
            string n = new string(lee.ReadLine()); 
            string[] o = n.Split(' ');
            for (int i = 0; i < balas.num; i++) //se leen las filas de cada una de las balas
            {
                balas.ent[i].fil = int.Parse(o[i]);
            }
            lee.Close();
        }
        static char LeeInput()
        {
            char ch = ' ';
            if (Console.KeyAvailable)
            {
                string dir = Console.ReadKey(true).Key.ToString();
                if (dir == "A" || dir == "LeftArrow") ch = 'l';
                else if (dir == "D" || dir == "RightArrow") ch = 'r';
                else if (dir == "W" || dir == "UpArrow") ch = 'u';
                else if (dir == "S" || dir == "DownArrow") ch = 'd';
                else if (dir == "X" || dir == "Spacebar") ch = 'x'; // bala        
                else if (dir == "P") ch = 'p'; // pausa					
                else if (dir == "Q" || dir == "Escape") ch = 'q'; // salir
                while (Console.KeyAvailable) Console.ReadKey(true);
            }
            return ch;
        }
    }
}