using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Podemos gestionar varias listas de clips (e incluir nuevos) utilizando ScriptableObjects.
/// SoundLib es un ScriptableObject que actúa como una biblioteca de AudioClips.
/// Permite inicializar un diccionario para acceder rápidamente a los sonidos por su nombre.
/// </summary>
[CreateAssetMenu(fileName = "new SoundLib", menuName = "Scriptable Objects/Sound Lib")]
public class SoundLib : ScriptableObject
{
	/// <summary>
	/// Lista de entradas de sonidos, donde cada entrada contiene un nombre y un AudioClip.
	/// </summary>
	public List<Sound> soundEntries;

	/// <summary>
	/// Diccionario interno que mapea los nombres de los sonidos a sus respectivos AudioClips.
	/// </summary>
	private Dictionary<string, AudioClip> soundDictionary;

	/// <summary>
	/// Inicializa el diccionario de sonidos a partir de la lista de entradas de AudioClips.
	/// </summary>
	public void Initialize()
	{
		soundDictionary = new Dictionary<string, AudioClip>();

		foreach (Sound s in soundEntries)
		{
			if (!soundDictionary.ContainsKey(s.soundName)) soundDictionary.Add(s.soundName, s.clip);
		}
	}

	/// <summary>
	/// Obtiene un AudioClip del diccionario utilizando su nombre.
	/// </summary>
	/// <param name="name">El nombre del AudioClip que se desea buscar.</param>
	/// <returns>El AudioClip correspondiente al nombre, o null si no se encuentra.</returns>
	public AudioClip GetClip(string name)
	{
		if (soundDictionary == null) Initialize();

		if (soundDictionary.TryGetValue(name, out var clip)) return clip;

		Debug.LogWarning($"Sound '{name}' not found in library.");
		return null;
	}
}