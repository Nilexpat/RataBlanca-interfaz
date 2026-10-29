# Proyecto: [por confirmar — ¿"La bitácora"?]

## Contexto
- Qué es: app interactiva del universo transmedia "Rata Blanca". Es la bitácora del laboratorio: el usuario explora Rat City, sus misterios (datos en gráficos), sus personajes y sus objetos.
- Materia / entrega / fecha límite: [por confirmar]
- Rúbrica: [por confirmar]

## Técnico
- Versión de Unity: 6000.3.9f1 (Unity 6.3) — confirmado en el proyecto
- Render pipeline: URP 17.3 — confirmado en el proyecto
- Plataforma: WebGL, publicado en GitHub Pages. Configuración de publicación estándar.
  - Activar "Decompression Fallback" o usar compresión Gzip/desactivada, porque GitHub Pages no envía los encabezados de Brotli.
  - En WebGL, Application.Quit() no funciona.
  - Los navegadores no reproducen audio hasta el primer clic del usuario. El sonido ambiente arranca después de la primera interacción.
- Resolución de referencia: [por confirmar, la de los frames de Figma]
- Sistema de UI: uGUI (Canvas) (sugerencia: el shader de ruido y los efectos de vidrio funcionan mejor con materiales en uGUI). [por confirmar]
- Paquetes: no usar paquetes pagos ni de terceros para los gráficos; dibujarlos con uGUI. Instalados: uGUI 2.0 (incluye TextMeshPro), Input System 1.18, URP 17.3.
- Entrada: el proyecto usa SOLO el Input System nuevo (Escape se lee con Keyboard.current; el EventSystem usa InputSystemUIInputModule).
- Ojo: el proyecto está en compresión Brotli sin Decompression Fallback → hay que cambiarlo antes del build para GitHub Pages.

## Pantallas y flujo
1. **Menú principal**: Jugar / Nuevo juego / Salir.
   - Nuevo juego: reinicia el progreso y muestra el tutorial.
   - Jugar: [por confirmar: ¿continúa sin tutorial?]
   - Salir: como Application.Quit() no funciona en WebGL, muestra un mensaje de cierre o vuelve a la pantalla de inicio. [por confirmar]
2. **Interfaz principal del juego**: barra lateral con 6 opciones. Cada sección se abre con un clic y se cierra con Escape o con el botón Volver.
   1. **Mapa**: pendiente, por ahora solo un panel vacío.
   2. **Misterios**: gráficos desplegables (ver Gráficos).
   3. **Personajes**: carrusel de 4 personajes (ver Personajes).
   4. **Objeto**: el turbante de Madame White (ver Objeto).
   5. **Configuración**: volúmenes (ver Audio).
   6. **Salir del juego**: vuelve al menú principal.
3. **Tutorial** (al empezar un juego nuevo): pasos cortos que señalan dónde hacer clic. Ejemplo: "Haz clic aquí para abrir Misterios" → el usuario lo abre → "Presiona Escape para volver" → el usuario vuelve → siguiente sección. Termina cuando el usuario ya se movió entre las pantallas.
   - En WebGL a pantalla completa, Escape saca de la pantalla completa. Por eso también va un botón "Volver" visible.

## Gráficos (sección Misterios), lo principal
- Unos 5 bloques desplegables tipo acordeón. Cada bloque tiene un título y un ícono que indica el tipo de gráfico.
- Animación al expandirse y al contraerse, con un sonido para cada acción.
- Al desplegarse, el gráfico se anima: las barras crecen y la torta se llena.
- Tipos de gráfico:
  - Torta / dona
  - Barras verticales
  - Barras horizontales (de izquierda a derecha)
  - Futuro: 2 gráficos de líneas / variables. La arquitectura debe permitir agregarlos sin rehacer lo demás.
- Todo editable desde el Inspector, sin tocar código (sugerencia: un ScriptableObject por gráfico):
  - Tipo de gráfico, título e ícono.
  - Lista de datos: etiqueta, valor y color.
  - Etiquetas de los ejes X e Y.
  - Activar o desactivar la leyenda.
  - Duración y curva de la animación.

## Personajes
- Carrusel con 4 personajes: imagen, nombre y descripción.
- Botones anterior y siguiente en bucle: después del 4 vuelve al 1, y antes del 1 va al 4.
- Datos editables desde el Inspector. Usar estos textos por defecto:
  - **Rata Blanca:** Una rata de laboratorio de pelaje blanco impecable y mirada inquieta. Empezó siendo un romántico sin remedio: dramático, soñador, capaz de hacer un poema de cualquier gesto de Antonella. Cree que los dioses son seres divinos y que todo lo que pasa en Rat City tiene un sentido. Pero después de esa noche y de un experimento del que nadie habla, algo cambió en él. Ahora es frío, sarcástico y desconfiado, y solo lo mueve una idea: a Antonella no la mató la naturaleza, alguien la envenenó.
  - **Antonella:** Una rata procreadora, diseñada para dar vida, de una belleza que hacía que hasta los dioses la trataran con cuidado. Detrás de esa delicadeza había una inteligencia poco común, pero no le alcanzó para sobrevivir a Rat City. Nunca sintió nada por Rata Blanca. Aun así, desde el más allá quizás le esté agradecida por todo lo que él sigue haciendo por ella, sin que ella esté ahí para verlo.
  - **Madame White:** Una dama de la alta sociedad de Rat City, elegante y de modales impecables, que nunca se quita su turbante. Dice escuchar a los dioses y habla como si ya hubiera leído el final de todas las historias. Sus consejos suenan a adorno hasta que se cumplen. Cree que el destino ya está escrito y que solo unos pocos pueden leerlo. Lo que nadie sabe es cuánto le está costando ese don: con el tiempo, el turbante le ha ido quitando la cabeza.
  - **El Gordo John:** Una rata enorme, de pelaje manchado y con calvas, que huele a orines y tose más de lo que habla. No se para de su silla en la taberna más sucia de Rat City. Es cínico, desafiante y no le debe nada a nadie, menos a Antonella, a quien nunca soportó. Antes fue el arquitecto más brillante de la ciudad y creyó que los dioses castigaban a los traidores. Hoy solo cree en la cerveza y en no hacer preguntas. Dicen que es el único que nunca dejó de ir a la taberna.

## Objeto: el turbante de Madame White (vista de despiece)
**Imágenes** (en Assets/Imagenes, sin tilde; importarlas como Sprite (2D and UI) y respetar la transparencia):
- **Turbante** (Turbante.png, 1186×982): el objeto principal.
- **Rubi** (Rubi.png): el rubí separado. Va superpuesto exactamente encima del rubí de la imagen Turbante, para poder darle brillo propio.
- **RubiZoom** (RubiZoom.png): la vista circular del rubí transparente, que deja ver el componente tecnológico de adentro.
- **Tela** (Tela.png): el corte de la tela con sus 3 capas.

**Estado inicial**
- Solo se ven Turbante y Rubi.
- Hay dos zonas interactivas, cada una con un brillo suave que pulsa (glow) para indicar que se puede hacer clic:
  1. El rubí (Rubi).
  2. La tela (puede brillar todo el turbante).
- Al pasar el mouse sobre una zona, el brillo sube y suena el sonido de feedback.

**Al hacer clic en la tela**
- Aparece Tela con animación (escala y desvanecido), abajo a la izquierda del turbante.
- Salen 3 flechas, una por capa, cada una con su texto.

**Al hacer clic en el rubí**
- Aparece RubiZoom con animación, abajo a la derecha del turbante.
- Sale 1 flecha que señala el componente tecnológico de adentro del rubí, con su texto.
- (sugerencia) Una línea conecta el rubí del turbante con el círculo, como una lupa.

**Flechas y textos**
- Las flechas se dibujan en secuencia, una tras otra, y después aparece cada texto.
- Todo editable desde el Inspector:
  - Texto de cada etiqueta (por defecto "Capa 1", "Capa 2", "Capa 3" y "Componente"; yo pongo los textos finales).
  - Punto de origen y punto final de cada flecha.
  - Color y grosor de la flecha.
  - Intensidad y velocidad del brillo.
  - Duración de cada animación.
- Cada zona se abre y se cierra por separado.
- Escape o el botón Volver cierra las vistas abiertas con la animación al revés.
- Usa los mismos sonidos de expandir y contraer de los gráficos.

## Audio
- Un AudioManager con espacios en el Inspector donde arrastro mis archivos:
  - Sonido al pasar el mouse por un botón (hover).
  - Sonido al hacer clic.
  - Sonido al expandir.
  - Sonido al contraer.
  - Sonido ambiente de fondo, en bucle.
- AudioMixer con grupos General, Feedback y Ambiente.
- La pantalla de Configuración tiene 3 sliders (General, Feedback y Ambiente) que se guardan en PlayerPrefs.

## Estilo visual
- Estética de laboratorio científico. Paleta: [por confirmar: colores de Figma].
- Fondo con shader de ruido animado en el tiempo, que parezca una radiografía o escáner cerebral. [por confirmar: parámetros del shader de Figma]
- Barra lateral con efecto de vidrio, sombra interna y sombra externa. (sugerencia: el desenfoque real es costoso en WebGL; aceptar una versión simulada si va lento)

## Diseño adaptativo
- Toda la interfaz se adapta al tamaño de la ventana del navegador:
  - Canvas Scaler en "Scale With Screen Size", con la resolución de referencia de Figma.
  - Anchors bien configurados.
- La barra lateral se expande y se contrae con animación:
  - Al expandirse, el contenido de la pantalla se desplaza para hacerle espacio.
  - Al contraerse, el contenido vuelve a quedar centrado.
- Lo mismo con los bloques desplegables: al abrirse uno, los demás se reacomodan suavemente, sin saltos.
- Tomar como referencia el comportamiento de los frames de Figma.

## Datos
- Se guarda: si el tutorial ya se vio y los volúmenes.
- Dónde: PlayerPrefs.

## Reglas obligatorias
1. Funcionar en WebGL publicado en GitHub Pages.
2. Gráficos, sonidos, personajes y objeto configurables desde el Inspector.
3. Navegación: clic para abrir una sección, Escape o botón Volver para regresar.
4. La interfaz es adaptativa a cualquier tamaño de ventana.

## Reglas deseables
1. Gráficos de líneas / variables.
2. Sección Mapa.

## Convenciones de código
- Idioma de variables y clases: [por confirmar]
- Comentarios: [por confirmar]
- Carpetas: [por confirmar] (sugerencia: Assets/Scripts, Assets/UI, Assets/Audio, Assets/Data; las imágenes ya están en Assets/Imagenes; los sonidos irán en Assets/Sonidos)

## Qué NO tocar
- No mover ni renombrar las imágenes de Assets/Imagenes.
- [por confirmar: otras escenas o carpetas]

## Cómo sé que está terminado
- Se puede ir del menú al juego, recorrer las 6 opciones de la barra lateral y volver sin errores.
- Los gráficos se despliegan y contraen con animación y sonido.
- El carrusel de personajes funciona en bucle.
- En Objeto, el rubí y la tela brillan; al hacer clic se abren sus vistas con flechas y textos, y se cierran con Escape.
- La barra lateral se expande y se contrae, y el contenido se reacomoda.
- Los 3 sliders cambian el volumen y se guardan.
- El tutorial aparece al iniciar un juego nuevo.
- El build corre en GitHub Pages.