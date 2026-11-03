using System;
using System.Collections.Generic;
using UnityEngine;

namespace RataBlanca.Personajes
{
    [Serializable]
    public class CharacterEntry
    {
        public string name = "Personaje";
        [Tooltip("Imagen del personaje (fondo transparente).")]
        public Sprite portrait;
        [TextArea(4, 12)] public string description = "";

        public CharacterEntry() { }
        public CharacterEntry(string name, string description)
        {
            this.name = name;
            this.description = description;
        }
    }

    /// <summary>
    /// Personajes del carrusel (sección Personajes), en orden.
    /// Archivo: Assets/Data/Personajes.asset
    /// Para cambiar un nombre, una descripción o una imagen, edítalo aquí;
    /// con el Play activo se ve al instante y se conserva.
    /// </summary>
    [CreateAssetMenu(fileName = "Personajes", menuName = "Rata Blanca/Personajes")]
    public class CharacterRoster : ScriptableObject
    {
        public List<CharacterEntry> characters = new List<CharacterEntry>
        {
            new CharacterEntry("Rata Blanca",
                "Una rata de laboratorio de pelaje blanco impecable y mirada inquieta. Empezó siendo un romántico sin remedio: dramático, soñador, capaz de hacer un poema de cualquier gesto de Antonella. Cree que los dioses son seres divinos y que todo lo que pasa en Rat City tiene un sentido. Pero después de esa noche y de un experimento del que nadie habla, algo cambió en él. Ahora es frío, sarcástico y desconfiado, y solo lo mueve una idea: a Antonella no la mató la naturaleza, alguien la envenenó."),
            new CharacterEntry("Antonella",
                "Una rata procreadora, diseñada para dar vida, de una belleza que hacía que hasta los dioses la trataran con cuidado. Detrás de esa delicadeza había una inteligencia poco común, pero no le alcanzó para sobrevivir a Rat City. Nunca sintió nada por Rata Blanca. Aun así, desde el más allá quizás le esté agradecida por todo lo que él sigue haciendo por ella, sin que ella esté ahí para verlo."),
            new CharacterEntry("El Gordo John",
                "Una rata enorme, de pelaje manchado y con calvas, que huele a orines y tose más de lo que habla. No se para de su silla en la taberna más sucia de Rat City. Es cínico, desafiante y no le debe nada a nadie, menos a Antonella, a quien nunca soportó. Antes fue el arquitecto más brillante de la ciudad y creyó que los dioses castigaban a los traidores. Hoy solo cree en la cerveza y en no hacer preguntas. Dicen que es el único que nunca dejó de ir a la taberna."),
            new CharacterEntry("Madame White",
                "Una dama de la alta sociedad de Rat City, elegante y de modales impecables, que nunca se quita su turbante. Dice escuchar a los dioses y habla como si ya hubiera leído el final de todas las historias. Sus consejos suenan a adorno hasta que se cumplen. Cree que el destino ya está escrito y que solo unos pocos pueden leerlo. Lo que nadie sabe es cuánto le está costando ese don: con el tiempo, el turbante le ha ido quitando la cabeza."),
        };

        [Header("Tamaño")]
        [Tooltip("Escala de las imágenes respecto a su tamaño original. Es la misma para todos, así se respeta la diferencia de tamaño entre personajes.")]
        public float portraitScale = 1.15f;

        [Header("Animación del cambio de personaje")]
        [Tooltip("Cuánto se desliza el personaje al salir y entrar (px).")]
        public float slideDistance = 140f;
        [Tooltip("Duración de la salida (s).")]
        public float outDuration = 0.18f;
        [Tooltip("Duración de la entrada (s).")]
        public float inDuration = 0.4f;
        [Tooltip("Velocidad del vaivén suave del personaje (0 = quieto).")]
        public float idleBobSpeed = 1.1f;
        [Tooltip("Altura del vaivén (px).")]
        public float idleBobHeight = 6f;

        [NonSerialized] public int version;
        void OnValidate() => version++;
    }
}
