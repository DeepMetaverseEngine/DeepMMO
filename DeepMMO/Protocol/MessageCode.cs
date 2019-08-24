using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMMO.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepMMO.Protocol
{

    public class MessageCodeManager
    {
        public static MessageCodeManager Instance { get; private set; }
        public ISerializerFactory Factory { get => factory; }
        private readonly ISerializerFactory factory;
        private readonly HashMap<int, ResponseErrorCodes> id_codes = new HashMap<int, ResponseErrorCodes>();
        private readonly HashMap<string, ResponseErrorCodes> type_codes = new HashMap<string, ResponseErrorCodes>();
        private Properties text_codes;
        public MessageCodeManager(ISerializerFactory factory)
        {
            Instance = this;
            this.factory = factory;
            foreach (var codec in factory.AllTypes)
            {
                var codes = new ResponseErrorCodes(codec.MessageType);
                type_codes.Add(codes.FullName, codes);
                id_codes.Add(codec.MessageID, codes);
            }
        }
        public virtual void Load(string path)
        {
            this.text_codes = Properties.LoadFromResource(path);
            if (text_codes != null)
            {
                foreach (var codes in type_codes.Values)
                {
                    var sub = text_codes.SubProperties(codes.FullName + ":");
                    codes.Load(sub);
                }
            }
        }
        public virtual void Save(string path)
        {
            Properties prop = new Properties();
            foreach (var e in type_codes)
            {
                var sub = e.Value.Save();
                prop.PutAll(sub, e.Key + ":");
            }
            var text = prop.ToString();
            CFiles.CreateFile(new System.IO.FileInfo(path));
            System.IO.File.WriteAllText(path, path, CUtils.UTF8_BOM);
        }

        //------------------------------------------------------------------------------------------------------------

        public virtual string GetCodeMessage(Response rsp)
        {
            return GetCodeMessage(rsp.GetType(), rsp.s2c_code);
        }
        public virtual string GetCodeMessage(Type type, int s2c_code)
        {
            var typecodes = type_codes.Get(type.ToTypeDefineFullName());
            if (typecodes != null)
            {
                return typecodes.GetCodeMessage(s2c_code);
            }
            return string.Empty;
        }
        public virtual string GetCodeMessage(string typeName, int s2c_code)
        {
            if (type_codes.TryGetValue(typeName, out var codes))
            {
                return codes.GetCodeMessage(s2c_code);
            }
            if (text_codes != null && text_codes.TryGetValue($"{typeName}:{s2c_code}", out var text))
            {
                return text;
            }
            return string.Empty;
        }

        //------------------------------------------------------------------------------------------------------------

        public class ResponseErrorCodes
        {
            protected HashMap<int, ErrorCode> error_codes = new HashMap<int, ErrorCode>();
            public string FullName { get; private set; }
            public ResponseErrorCodes(Type type)
            {
                this.FullName = type.ToTypeDefineFullName();
                using (var list =  CollectionObjectPool<ErrorCode>.AllocList())
                {
                    var fields = PropertyUtil.GetFields(type);
                    foreach (var field in fields)
                    {
                        if (field.IsLiteral && field.IsStatic && field.FieldType == typeof(int))
                        {
                            var attr = PropertyUtil.GetAttribute<MessageCodeAttribute>(field);
                            if (attr != null)
                            {
                                var code = (int)field.GetValue(null);
                                list.Add(new ErrorCode(type, code, attr));
                            }
                        }
                    }
                    foreach (var error_code in list)
                    {
                        try
                        {
                            error_codes.TryAdd(error_code.Code, error_code);
                        }
                        catch
                        {
                            throw new Exception($"AddErrorCodeError: Type={type.FullName} Code={error_code.Code}");
                        }
                    }
                }
            }
            internal void Load(Properties text)
            {
                foreach (var e in text)
                {
                    if (int.TryParse(e.Key, out var id))
                    {
                        if (error_codes.TryGetValue(id, out var code))
                        {
                            code.Load(e.Value);
                        }
                    }
                }
            }
            internal Properties Save()
            {
                Properties ret = new Properties();
                foreach (var e in error_codes)
                {
                    ret[e.Key.ToString()] = e.Value.Save();
                }
                return ret;
            }
            internal string GetCodeMessage(Response rsp)
            {
                if (error_codes.TryGetValue(rsp.s2c_code, out var code))
                {
                    return code.GetCodeMessage(rsp);
                }
                return null;
            }
            internal string GetCodeMessage(int s2c_code)
            {
                if (error_codes.TryGetValue(s2c_code, out var code))
                {
                    return code.GetCodeMessage();
                }
                return null;
            }
        }
        public class ErrorCode
        {
            protected readonly int code;
            protected readonly MessageCodeAttribute owner_attribute;
            protected readonly FieldInfo[] args;
            protected readonly object[] args_str;
            private string message;
            public int Code { get { return code; } }
            public MessageCodeAttribute CodeAttribute { get { return owner_attribute; } }
            public FieldInfo[] ArgsField { get { return args; } }
            public ErrorCode(Type owner_type, int code, MessageCodeAttribute attr)
            {
                this.code = code;
                this.owner_attribute = attr;
                this.message = attr.Message;
                if (attr.Args != null)
                {
                    using (var list = CollectionObjectPool<FieldInfo>.AllocList(attr.Args.Length))
                    {
                        foreach (var fileName in attr.Args)
                        {
                            var field = owner_type.GetField(fileName);
                            if (field == null)
                            {
                                throw new Exception(string.Format("错误代码文字字段不存在: Type={0} Code={1} FieldName={2}", owner_type, code, fileName));
                            }
                            list.Add(field);
                        }
                        this.args = list.ToArray();
                        this.args_str = new object[args.Length];
                    }
                }
            }
            internal void Load(string text)
            {
                this.message = text;
            }
            internal string Save()
            {
                return this.message;
            }
            public string GetCodeMessage(Response rsp)
            {
                try
                {
                    if (args == null) return message;
                    for (int i = 0; i < args.Length; i++)
                    {
                        args_str[i] = args[i].GetValue(rsp);
                    }
                    return string.Format(message, args_str);
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("错误代码文字错误：Type={0} Code={1} Error={2}", rsp.GetType(), rsp.s2c_code, err.Message), err);
                }
            }
            public string GetCodeMessage()
            {
                return message;
            }
        }
    }

}
