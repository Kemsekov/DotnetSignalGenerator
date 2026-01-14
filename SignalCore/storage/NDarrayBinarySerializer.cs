using System.Numerics;
using SQLite;
namespace SignalCore.Storage;

[DataModel]
public class NDarrayBinaryDataModel
{
    public NDarrayBinaryDataModel()
    {
        SetNdarray(np.array(new[]{0.0}));
    }
    [PrimaryKey, AutoIncrement]
    public long Id{get;set;}
    public byte[] DataReal { get; set; } = [];
    public byte[] DataImag { get; set; } = [];
    public float SignalMinReal{get;set;}=0;
    public float SignalMaxReal{get;set;}=0;
    public float SignalMinImag{get;set;}=0;
    public float SignalMaxImag{get;set;}=0;
    public string DataShape{get;set;}="";
    ndarray? _data = null;
    public ndarray GetNdarray()
    {
        if(_data is not null) return _data;
        if(SignalMinReal>SignalMaxReal || DataShape=="")
            throw new ArgumentException("Object was not properly initialized");
        var newWidthReal = (SignalMaxReal-SignalMinReal)/ushort.MaxValue;
        var newWidthImag = (SignalMaxImag-SignalMinImag)/ushort.MaxValue;
        
        var transformedReal = DataReal.ToShortArray().Select(v=>v*newWidthReal+SignalMinReal);
        var transformedImag = DataImag.ToShortArray().Select(v=>v*newWidthImag+SignalMinImag);
        
        var shape = DataShape.Split(' ').Select(long.Parse).ToArray();

        if(DataImag.Length==0){
            _data = np.array(transformedReal.ToArray(),np.Float32,copy:false).reshape(shape);
        }
        else
        {
            if(SignalMinImag>SignalMaxImag)
                throw new ArgumentException("Object was not properly initialized");
            var complex = transformedReal.Zip(transformedImag).Select(i=>new Complex(i.First,i.Second));
            _data = np.array(complex.ToArray(),np.Complex,copy:false).reshape(shape);
        }
        return _data;
    }
    public void SetNdarray(ndarray value)
    {
        if(value.shape.iDims.Length==0 || value.numel()==0) return;
        DataShape = string.Join(' ',value.shape.iDims);
        SignalMinReal = np.min(value).single();
        SignalMaxReal = np.max(value).single();
        SignalMinImag=-1;
        SignalMaxImag=-1;

        var widthReal = 1/(SignalMaxReal-SignalMinReal)*ushort.MaxValue;
        var reals = value.AsFloatArray();
        DataReal = 
            reals
            .Select(v=>(ushort)((v-SignalMinReal)*widthReal))
            .ToBinaryArray(reals.Length);

        if (value.Dtype == np.Complex)
        {
            var imag = value.Imag;
            SignalMinImag=np.min(imag).single();
            SignalMaxImag=np.max(imag).single();
            var widthImag = 1/(SignalMaxImag-SignalMinImag)*ushort.MaxValue;
            var imags = imag.AsFloatArray();
            DataImag = 
                imags
                .Select(v=>(ushort)((v-SignalMinImag)*widthImag))
                .ToBinaryArray(imags.Length);
        }
    }
}
