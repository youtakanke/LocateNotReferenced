using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// どこからも参照の無いオブジェクト見つける
/// 
/// 「Select DepenDensies」 の逆
/// 
/// 外部シェルを叩く
/// 
/// 使いかた
/// 
/// ・メニュー > window > LocateNoDependencies
/// ・project内で画像、material、prefab、AnimationClipのいずれか一つを選択して、
///  search ボタンを押す
/// </summary>
public class LocateNotReferenced : EditorWindow
{
	/// <summary>
	/// metaのguidを取得し、grep処理をするシェルファイルパス
	/// </summary>
	static readonly string SHELL_PATH = "/Editor/LocateNoDependencies/LocateNotReferenced.sh";
	/// <summary>
	/// grepによって見つかったファイルパスリスト
	/// </summary>
	private List <string> findObjectPathList = new List<string> ();
	/// <summary>
	/// 依存materialのリスト
	/// </summary>
	private List <Material> materialsList = new List<Material> ();
	/// <summary>
	/// 依存prefabのリスト
	/// </summary>
	private List <GameObject> prefabsList = new List<GameObject> ();

	[MenuItem ("Window/LocateNotReferenced")]
	private static void Init ()
	{
		LocateNotReferenced w = EditorWindow.GetWindow<LocateNotReferenced> ();
		w.Show ();
		w.title = "LocateNotReferenced";

	}

	private void OnGUI ()
	{
		//選択されているオブジェクトを取得
		Object[] selectObjects = Selection.GetFiltered (typeof(Object), SelectionMode.DeepAssets);

		if (GUILayout.Button ("Search") && selectObjects.Length > 0) {
			//シェルに渡す、grep対象ファイルのパス
			string filePathsString = "";

			materialsList.Clear ();
			prefabsList.Clear ();

			Object selectObj = selectObjects [0];
			filePathsString = filePathsString + " " + AssetDatabase.GetAssetPath (selectObj) + ".meta";

			//どのtypeのオブジェクトが選択されているか判定
			if (selectObj is Texture || selectObj is Shader) {
				//テクスチャ系ならmaterial検索とprefab検索
				SearchMaterial (selectObj, filePathsString);
				SearchPrefab (selectObj, filePathsString);
			}
			//materialかprefabならprefab検索
			if (selectObj is GameObject || selectObj is Material || selectObj is AnimationClip) {
				SearchPrefab (selectObj, filePathsString);
			}
		}

		//何も選択されていなければ
		if (selectObjects.Length == 0) {
			GUILayout.Label ("project上の対象となるAssetを選択してから\nsearchボタンお押してください。");
		}


		//↓以下結果を表示


		//依存materialを表示
		if (materialsList.Count > 0) {
			GUILayout.Label ("◆DepenDensies materials ↓↓");
			materialsList.ForEach (item => {
				EditorGUILayout.ObjectField (item, typeof(Material), false, GUILayout.Width (188f));
			});

		} else {
			GUILayout.Label ("※どのmaterialからも参照されていません。");
		}

		//依存prefabを表示
		if (prefabsList.Count > 0) {
			GUILayout.Label ("◆DepenDensies prefabs ↓↓");
			prefabsList.ForEach (item => {
				EditorGUILayout.ObjectField (item, typeof(GameObject), false, GUILayout.Width (188f));
			});
		} else {
			GUILayout.Label ("※どのprefabからも参照されていません。");
		}
	}

	/// <summary>
	/// 参照のあるマテリアルを抽出
	/// </summary>
	/// <param name="selectObj">Select object.</param>
	/// <param name="filePathsString">File paths string.</param>
	private void SearchMaterial (Object selectObj, string filePathsString)
	{
		LocateStart (".mat", filePathsString);//↑シェル実行　プロセス終了までまつ　　コルーチンのようなもの
		if (findObjectPathList.Count == 0) {
			//該当するmaterialがなかった
			UnityEngine.Debug.Log (selectObj.name + "はどのmaterialからも参照されていません。");
			return;
		} else {
			//該当するmaterialがあった
			materialsList = CreateDependenciesObjectList<Material> (out filePathsString);
		}
	}

	/// <summary>
	/// 参照のあるprefabを抽出
	/// </summary>
	/// <param name="selectObj">Select object.</param>
	/// <param name="filePathsString">File paths string.</param>
	private void SearchPrefab (Object selectObj, string filePathsString)
	{
		LocateStart (".prefab", filePathsString);//↑シェル実行　プロセス終了までまつ　　　コルーチンのようなもの

		if (findObjectPathList.Count == 0) {
			//該当するprefabがなかった
			UnityEngine.Debug.Log (selectObj.name + "はどのprefabからも参照されていません。");
			return;
		} else {
			//該当するprefabがあった
			prefabsList = CreateDependenciesObjectList<GameObject> (out filePathsString);
		}
	}

	/// <summary>
	/// 見つけ出す
	/// LocateNoDependencies.sh のシェルを叩く
	/// </summary>
	/// <param name="extension">grep対象の拡張子.</param>
	/// <param name="arguments">ファイルパス.</param>
	private void LocateStart (string extension, string arguments)
	{
		findObjectPathList.Clear ();
		var p = new Process ();
		p.StartInfo = new ProcessStartInfo () {
			FileName = "/bin/sh",
			Arguments = Application.dataPath + SHELL_PATH + " " + extension + " " + arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardInput = true,
			RedirectStandardError = true,
		};

		// シェルの結果が返ってくる
		// grep処理によって引っかかったファイル名をfindObjectPathListに格納
		p.OutputDataReceived += (object sender, DataReceivedEventArgs args) => {
			if (!string.IsNullOrEmpty (args.Data)) {
				if (args.Data.ToString ().Contains ("./Assets")) {
					findObjectPathList.Add (args.Data);
				}
			}
		};

		//シェルのエラー時
		p.ErrorDataReceived += (object sender, DataReceivedEventArgs args) => {
			if (!string.IsNullOrEmpty (args.Data)) {
				UnityEngine.Debug.LogError (args.Data);
			}
		};

		// プロセス終了時.
		p.Exited += (object sender, System.EventArgs e) => {
			Process proc = (System.Diagnostics.Process)sender;
			// プロセスを閉じる.
			proc.Kill ();
		};

		p.Start ();
		p.BeginOutputReadLine ();
		// プロセスエラー出力.
		p.BeginErrorReadLine ();

		//処理終了まで待つ　引数：タイムアウト時間
		p.WaitForExit (5 * 1000); //これ、コルーチンのようなもの
	}

	/// <summary>
	/// 依存しているオブジェクトのリストを返す
	/// </summary>
	/// <returns>The object list.</returns>
	/// <param name="files">Files.</param>
	/// <typeparam name="T">The 1st type parameter.</typeparam>
	private List<T> CreateDependenciesObjectList<T> (out string files)
	{
		var _list = new List<T> ();
		files = "";
		for (int i = 0; i < findObjectPathList.Count; i++) {
			files = files + " " + findObjectPathList [i] + ".meta";
			Object _obj = AssetDatabase.LoadAssetAtPath (findObjectPathList [i].Replace ("./", ""), typeof(T));
			_list.Add ((T)System.Convert.ChangeType (_obj, typeof(T)));
		}
		return _list;
	}
}

