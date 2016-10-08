//===========================================================
//  Author  :しょった
//  Summary :NxM行列の計算をするためのクラス
//===========================================================

//9x9行列
public class MatrixNxM 
{
	//行列
	//dim0	:縦
	//dim1	:横
	public double[,] Mat{ get; private set; }
	public MatrixNxM(int n, int m)
	{
		Mat = new double[n, m];
	}
	public MatrixNxM(MatrixNxM matrix)
	{
		int n, m;
		n = matrix.Mat.GetLength(0);
		m = matrix.Mat.GetLength(1);
		Mat = new double[n, m];
		for (int i = 0; i < n; i++)
			for (int j = 0; j < m; j++)
				Mat [i, j] = matrix.Mat [i, j];
	}

	//零行列
	public static MatrixNxM ZeroMatrix(int n, int m)
	{
		MatrixNxM c = new MatrixNxM (n, m);
		for (int i=0; i<n; i++)
			for(int j=0; j<m; j++)
				c.Mat[i,j] = 0.0f;
		return c;
	}
	//単位行列
	public static MatrixNxM UnitMatrix(int n)
	{
		MatrixNxM c = new MatrixNxM (n, n);
		for (int i=0; i<n; i++)
			for(int j=0; j<n; j++)
				c.Mat[i,j] = (i==j?1.0f:0.0f);
		return c;
	}
	//転値行列
	public MatrixNxM TransMatrix()
	{
		MatrixNxM a = new MatrixNxM(this);
		int n, m;
		n = a.Mat.GetLength (0);
		m = a.Mat.GetLength (1);
		MatrixNxM c = new MatrixNxM (m, n);
		for (int i=0; i<m; i++)
			for (int j=0; j<n; j++)
				c.Mat [i,j] = a.Mat [j,i];
		return c;
	}

	//行列掛け算
	//※aの横列とbの縦列のサイズが揃わなければNullを返す
	public static MatrixNxM MaltiplyMatrix(MatrixNxM a, MatrixNxM b)
	{
		if (a.Mat.GetLength (1) != b.Mat.GetLength (0))
			return null;
		
		int n, m, l;
		n = a.Mat.GetLength (0);	//縦サイズ
		m = b.Mat.GetLength (1);	//横サイズ
		l = a.Mat.GetLength (1);	//足し合わせる項数
		MatrixNxM c = new MatrixNxM (n, m);
		for (int i=0; i<n; i++)
			for (int j=0; j<m; j++) {
				c.Mat [i,j]=0.0f;
				for (int k=0; k<l; k++)
					c.Mat [i,j] += a.Mat [i,k] * b.Mat[k,j];
			}
		return c;
	}
	public static MatrixNxM operator * (MatrixNxM a, MatrixNxM b)
	{
		return MatrixNxM.MaltiplyMatrix (a, b);
	}

	//逆行列
	public MatrixNxM InverseMatrix()
	{
		MatrixNxM a = new MatrixNxM(this);
		if (a.Mat.GetLength (0) != a.Mat.GetLength (1)) {
			//非正方行列なら擬似逆行列を求める
			return (a.TransMatrix () * a).InverseMatrix() * a.TransMatrix();
		} else {
			//掃き出し法(http://thira.plavox.info/blog/2008/06/_c.html を参考)
			int n = a.Mat.GetLength (0);
			MatrixNxM c = MatrixNxM.UnitMatrix (n);
			int correctRows = 0;
			double buf = 0.0f;
			for(int i=0;i<n;i++){
				//a.Mat[i, l] != 0.0f(l>=i)となる行lを探す
				int l = i;
				while (l < n) {
					if (a.Mat [l, i] != 0.0f)
						break;
					l++;
				}
				//見つからなければ次の行を処理
				if (l == n) continue;
				//l行目とi行目を入れ替え
				for (int k = 0; k < n; k++)
				{
					buf = a.Mat [l, k];
					a.Mat [l, k] = a.Mat [i, k];
					a.Mat [i, k] = buf;
					buf = c.Mat [l, k];
					c.Mat [l, k] = c.Mat [i, k];
					c.Mat [i, k] = buf;
				}
				//a.Mat[i, i]==1となるようにi行目を割り算
				buf = a.Mat [i, i];
				for (int j = 0; j < n; j++) {
					a.Mat [i, j] /= buf;
					c.Mat [i, j] /= buf;
				}
				//a.Mat[j, i]==0となるように、(j行目)-(i行目)*a.Mat[j, i]をする
				for (int j = 0; j < n; j++) {
					if (i == j)
						continue;
					buf = a.Mat [j, i];
					for (int k = 0; k < n; k++) {
						a.Mat [j, k] -= a.Mat [i, k] * buf;
						c.Mat [j, k] -= c.Mat [i, k] * buf;
					}
				}
				correctRows++;
			}
			if (correctRows < n)
				c = null;
			return c;
		}
	}

	//行列のデータを返す
	public override string ToString ()
	{
		int n, m;
		n = Mat.GetLength (0);
		m = Mat.GetLength (1);
		string matStr = "";
		for (int i = 0; i < n; i++) {
			matStr += "|";
			for (int j = 0; j < m; j++) {
				double num = Mat [i, j];
				matStr += num.ToString ("G4");
				if (j < m-1) matStr += " ";
			}
			matStr += "|\n";
		}

		return "[MatrixNxM:" + n + "x" + m + "] =\n" + matStr;
	}
}