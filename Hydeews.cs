using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Services;

namespace hydeews
{
	// Token: 0x02000002 RID: 2
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[WebService(Namespace = "hydeesoft/")]
	[ToolboxItem(false)]
	public class Hydeews : WebService
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		[WebMethod]
		public string debug(string sql)
		{
			return "";
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002068 File Offset: 0x00000268
		[WebMethod]
		public int isconnected(string verify_client, out string error)
		{
			error = "";
			if (Hydeews.verify_host == "UnLoad(Null)")
			{
				string tmp;
				int result = this.getverify(out tmp);
				if (result != 1)
				{
					error = "主机取校验码失败。可能未能正确连接数据库。";
					return -1;
				}
				Hydeews.verify_host = tmp;
			}
			int result2;
			if (!this.checkverify(verify_client))
			{
				error = "核对校验码失败。";
				result2 = -1;
			}
			else
			{
				result2 = 1;
			}
			return result2;
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020DC File Offset: 0x000002DC
		[WebMethod]
		public int executesql(string verify_local, string userid, int requesttype, int compressionlevel, int timeout, int maxsize, byte[] input, bool inputiscompression, int pagesize, out byte[] outdata, out bool outiscompression, out int allsize, out int sended, out int pagehandle, out string pagepwd, out string error)
		{
			outdata = Encoding.GetEncoding("gb2312").GetBytes("");
			outiscompression = false;
			allsize = 0;
			sended = 0;
			pagehandle = -1;
			pagepwd = "";
			error = "";
			string resultfile = "";
			string resultstring = "";
			int result;
			if (requesttype == 10)
			{
				string sql;
				if (inputiscompression)
				{
					string tmpfile = this.GetTmpFile();
					if (this.SaveBytesToFile(input, tmpfile) != 1)
					{
						error = "解压文件错误：保存文件出错。";
						return -1;
					}
					if (this.ExpandFile(tmpfile, tmpfile + "txt", true) != 1)
					{
						error = "解压文件错误@executesql.simple。";
						return -1;
					}
					sql = Encoding.GetEncoding("gb2312").GetString(this.GetBytesFromFile(tmpfile + "txt"));
					File.Delete(tmpfile + "txt");
					if (sql == null)
					{
						error = "读取文件错误@executesql.simple。";
						return -1;
					}
				}
				else
				{
					sql = Encoding.GetEncoding("gb2312").GetString(input);
				}
				if (this.ExecSimple(sql, timeout, maxsize, out resultstring, out error) != 1)
				{
					result = -1;
				}
				else
				{
					outdata = Encoding.GetEncoding("gb2312").GetBytes(resultstring);
					allsize = outdata.Length;
					sended = allsize;
					result = 1;
				}
			}
			else if (requesttype == 2)
			{
				string sql;
				if (inputiscompression)
				{
					string tmpfile = this.GetTmpFile();
					if (this.SaveBytesToFile(input, tmpfile) != 1)
					{
						error = "解压文件错误：保存文件出错。";
						return -1;
					}
					if (this.ExpandFile(tmpfile, tmpfile + "txt", true) != 1)
					{
						error = "解压文件错误@executesql.cmdxml。";
						return -1;
					}
					sql = Encoding.GetEncoding("gb2312").GetString(this.GetBytesFromFile(tmpfile + "txt"));
					File.Delete(tmpfile + "txt");
					if (sql == null)
					{
						error = "读取文件错误@executesql.cmdxml。";
						return -1;
					}
				}
				else
				{
					sql = Encoding.GetEncoding("gb2312").GetString(input);
				}
				if (sql.Length > 500000)
				{
					error = "CmdXml模式要求传入的语句长度不能超过500K个字符。";
					result = -1;
				}
				else if (this.ExecCmdXml(sql, timeout, maxsize, out resultfile, out error) != 1)
				{
					result = -1;
				}
				else
				{
					FileInfo fi = new FileInfo(resultfile);
					if (fi.Length > (long)maxsize && maxsize > 0)
					{
						if (File.Exists(resultfile))
						{
							File.Delete(resultfile);
						}
						error = "结果超过" + maxsize.ToString() + "的长度限制。";
						result = -1;
					}
					else if (fi.Length >= (long)compressionlevel && compressionlevel > 0)
					{
						string cabfile = this.GetTmpFile();
						if (File.Exists(cabfile))
						{
							File.Delete(cabfile);
						}
						if (this.MakecabFile(resultfile, cabfile, true) != 1)
						{
							if (File.Exists(resultfile))
							{
								File.Delete(resultfile);
							}
							if (File.Exists(cabfile))
							{
								File.Delete(cabfile);
							}
							error = "压缩结果文件出错。";
							result = -1;
						}
						else
						{
							allsize = (int)fi.Length;
							sended = allsize;
							outiscompression = true;
							outdata = this.GetBytesFromFile(cabfile);
							File.Delete(cabfile);
							if (outdata == null)
							{
								error = "读取文件错误@executesql.cmdxml。";
								outdata = Encoding.GetEncoding("gb2312").GetBytes("");
								result = -1;
							}
							else
							{
								result = 1;
							}
						}
					}
					else
					{
						allsize = (int)fi.Length;
						sended = allsize;
						outiscompression = false;
						outdata = this.GetBytesFromFile(resultfile);
						File.Delete(resultfile);
						if (outdata == null)
						{
							error = "读取文件错误@executesql.cmdxml2。";
							outdata = Encoding.GetEncoding("gb2312").GetBytes("");
							result = -1;
						}
						else
						{
							result = 1;
						}
					}
				}
			}
			else if (requesttype == 1 && (inputiscompression || input.Length > 100000))
			{
				string sqlfile = this.GetTmpFile();
				if (inputiscompression)
				{
					string sqlcabfile = this.GetTmpFile();
					if (File.Exists(sqlcabfile))
					{
						File.Delete(sqlcabfile);
					}
					if (this.SaveBytesToFile(input, sqlcabfile) != 1)
					{
						error = "将压缩的SQL脚本存为文件失败。";
						return -1;
					}
					if (this.ExpandFile(sqlcabfile, sqlfile, true) != 1)
					{
						error = "解压缩执行文件失败。";
						if (File.Exists(sqlcabfile))
						{
							File.Delete(sqlcabfile);
						}
						if (File.Exists(sqlfile))
						{
							File.Delete(sqlfile);
						}
						return -1;
					}
				}
				else if (this.SaveBytesToFile(input, sqlfile) != 1)
				{
					error = "将SQL脚本存为文件失败。";
					return -1;
				}
				if (this.ExecCmdByFile(sqlfile, timeout, maxsize, out resultfile, out error) != 1)
				{
					result = -1;
				}
				else if (this.GetFirstPage(resultfile, pagesize, compressionlevel, out outdata, out outiscompression, out allsize, out sended, out pagehandle, out pagepwd, out error) != 1)
				{
					result = -1;
				}
				else
				{
					result = 1;
				}
			}
			else if (requesttype == 1 && !inputiscompression && input.Length <= 100000)
			{
				string sql = Encoding.GetEncoding("gb2312").GetString(input);
				if (this.ExecCmdByString(sql, timeout, maxsize, out resultfile, out error) != 1)
				{
					result = -1;
				}
				else if (this.GetFirstPage(resultfile, pagesize, compressionlevel, out outdata, out outiscompression, out allsize, out sended, out pagehandle, out pagepwd, out error) != 1)
				{
					result = -1;
				}
				else
				{
					result = 1;
				}
			}
			else
			{
				result = 1;
			}
			return result;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002718 File Offset: 0x00000918
		private int GetFirstPage(string filename, int pagesize, int compressionlevel, out byte[] outdata, out bool outiscompression, out int allsize, out int sended, out int pagehandle, out string pagepwd, out string error)
		{
			outdata = Encoding.GetEncoding("gb2312").GetBytes("");
			error = "";
			pagepwd = "";
			pagehandle = -1;
			sended = 0;
			allsize = 0;
			outiscompression = false;
			int result;
			if (!File.Exists(filename))
			{
				error = "文件已丢失。";
				result = -1;
			}
			else
			{
				FileInfo fi = new FileInfo(filename);
				if (pagesize == 0 || (long)pagesize >= fi.Length)
				{
					if (fi.Length >= (long)compressionlevel && compressionlevel > 0)
					{
						string filename_cab = this.GetTmpFile();
						if (this.MakecabFile(filename, filename_cab, true) != 1)
						{
							if (File.Exists(filename))
							{
								File.Delete(filename);
							}
							if (File.Exists(filename_cab))
							{
								File.Delete(filename_cab);
							}
							error = "压缩结果文件出错。";
							result = -1;
						}
						else
						{
							allsize = (int)fi.Length;
							sended = allsize;
							outiscompression = true;
							outdata = this.GetBytesFromFile(filename_cab);
							File.Delete(filename_cab);
							if (outdata == null)
							{
								error = "读取文件错误@getfirstpage";
								outdata = Encoding.GetEncoding("gb2312").GetBytes("");
								result = -1;
							}
							else
							{
								result = 1;
							}
						}
					}
					else
					{
						allsize = (int)fi.Length;
						sended = allsize;
						outiscompression = false;
						outdata = this.GetBytesFromFile(filename);
						File.Delete(filename);
						if (outdata == null)
						{
							error = "读取文件错误@getfirstpage2";
							outdata = Encoding.GetEncoding("gb2312").GetBytes("");
							result = -1;
						}
						else
						{
							result = 1;
						}
					}
				}
				else
				{
					byte[] FullData = this.GetBytesFromFile(filename);
					if (FullData == null)
					{
						error = "读取文件错误@getfirstpage3";
						result = -1;
					}
					else
					{
						allsize = FullData.Length;
						for (sended = pagesize; sended < allsize; sended++)
						{
							if (FullData[sended] == 13)
							{
								break;
							}
						}
						if (sended + 1 < allsize)
						{
							pagehandle = this.DistributeArray();
							if (pagehandle == -1)
							{
								error = "分配数组失败！";
								return -1;
							}
							Hydeews.storage_filelist[pagehandle] = filename;
							Hydeews.storage_lasttime[pagehandle] = DateTime.Now;
							pagepwd = Hydeews.storage_pwd[pagehandle];
						}
						Array.Resize<byte>(ref outdata, sended);
						Array.Copy(FullData, 0, outdata, 0, sended);
						sended++;
						if (sended >= compressionlevel && compressionlevel > 0)
						{
							string tmpfile_txt = this.GetTmpFile();
							File.Delete(tmpfile_txt);
							if (this.SaveBytesToFile(outdata, tmpfile_txt) != 1)
							{
								if (File.Exists(tmpfile_txt))
								{
									File.Delete(tmpfile_txt);
								}
								error = "保存文件失败。";
								result = -1;
							}
							else
							{
								string tmpfile = this.GetTmpFile();
								File.Delete(tmpfile);
								if (this.MakecabFile(tmpfile_txt, tmpfile, true) != 1)
								{
									if (File.Exists(tmpfile_txt))
									{
										File.Delete(tmpfile_txt);
									}
									if (File.Exists(tmpfile))
									{
										File.Delete(tmpfile);
									}
									error = "压缩文件失败。";
									result = -1;
								}
								else
								{
									int i = 0;
									while (this.IsFileOpened(tmpfile))
									{
										if (i > 100)
										{
											error = "打开文件失败。";
											return -1;
										}
										Thread.Sleep(10);
										i++;
									}
									outdata = this.GetBytesFromFile(tmpfile);
									if (File.Exists(tmpfile_txt))
									{
										File.Delete(tmpfile_txt);
									}
									if (File.Exists(tmpfile))
									{
										File.Delete(tmpfile);
									}
									if (outdata == null)
									{
										error = "读取文件错误@getfirstpage4。";
										outdata = Encoding.GetEncoding("gb2312").GetBytes("");
										result = -1;
									}
									else
									{
										outiscompression = true;
										result = 1;
									}
								}
							}
						}
						else
						{
							outiscompression = false;
							result = 1;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002B4C File Offset: 0x00000D4C
		[WebMethod]
		public int getapage(int pagehandle, string pagepwd, int compressionlevel, int pagesize, int sended_before, out byte[] outdata, out bool outiscompression, out int sended_after, out string error)
		{
			outdata = Encoding.GetEncoding("gb2312").GetBytes("");
			outiscompression = false;
			sended_after = 0;
			error = "";
			int result;
			if (pagehandle < 0 || pagehandle >= Hydeews.storage_using.Length)
			{
				error = "句柄无效。";
				result = -1;
			}
			else if (Hydeews.storage_pwd[pagehandle] != pagepwd)
			{
				error = "寄存的句柄已过期。";
				result = -1;
			}
			else if (!File.Exists(Hydeews.storage_filelist[pagehandle]))
			{
				error = "寄存的文件已丢失。";
				result = -1;
			}
			else
			{
				string filename = Hydeews.storage_filelist[pagehandle];
				byte[] FullData = this.GetBytesFromFile(filename);
				if (FullData == null)
				{
					error = "读取文件错误@getapage。";
					result = -1;
				}
				else
				{
					int allsize = FullData.Length;
					if (sended_before + pagesize + 1000 > allsize)
					{
						sended_after = allsize;
					}
					else
					{
						for (sended_after = sended_before + pagesize + 1; sended_after < allsize; sended_after++)
						{
							if (FullData[sended_after] == 13)
							{
								break;
							}
						}
						sended_after++;
					}
					if (sended_after >= allsize)
					{
						int i = 0;
						while (this.IsFileOpened(filename))
						{
							if (i > 100)
							{
								error = "打开文件失败。";
								return -1;
							}
							Thread.Sleep(10);
							i++;
						}
						File.Delete(filename);
						Hydeews.storage_filelist[pagehandle] = "";
						Hydeews.storage_pwd[pagehandle] = "";
						Hydeews.storage_using[pagehandle] = false;
					}
					Array.Resize<byte>(ref outdata, sended_after - sended_before - 1);
					Array.Copy(FullData, sended_before + 1, outdata, 0, sended_after - sended_before - 1);
					if (sended_after - sended_before >= compressionlevel && compressionlevel > 0)
					{
						string tmpfile_txt = this.GetTmpFile();
						File.Delete(tmpfile_txt);
						if (this.SaveBytesToFile(outdata, tmpfile_txt) != 1)
						{
							if (File.Exists(tmpfile_txt))
							{
								File.Delete(tmpfile_txt);
							}
							error = "保存文件失败。";
							result = -1;
						}
						else
						{
							int i = 0;
							while (this.IsFileOpened(tmpfile_txt))
							{
								if (i > 100)
								{
									error = "打开文件失败";
									return -1;
								}
								Thread.Sleep(10);
								i++;
							}
							string tmpfile = this.GetTmpFile();
							if (this.MakecabFile(tmpfile_txt, tmpfile, true) != 1)
							{
								if (File.Exists(tmpfile_txt))
								{
									File.Delete(tmpfile_txt);
								}
								if (File.Exists(tmpfile))
								{
									File.Delete(tmpfile);
								}
								error = "压缩文件失败。";
								result = -1;
							}
							else
							{
								outdata = this.GetBytesFromFile(tmpfile);
								if (File.Exists(tmpfile_txt))
								{
									File.Delete(tmpfile_txt);
								}
								if (File.Exists(tmpfile))
								{
									File.Delete(tmpfile);
								}
								if (outdata == null)
								{
									error = "读取文件错误@getapage2。";
									outdata = Encoding.GetEncoding("gb2312").GetBytes("");
									result = -1;
								}
								else
								{
									outiscompression = true;
									result = 1;
								}
							}
						}
					}
					else
					{
						outiscompression = false;
						result = 1;
					}
				}
			}
			return result;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002EA8 File Offset: 0x000010A8
		private int ExecCmdXml(string sql, int timeout, int maxsize, out string outfile, out string error)
		{
			outfile = "";
			error = "";
			this.ResetConnectionsFromSettings();
			string filename = this.GetTmpFile();
			string objfile = this.GetTmpFile();
			sql = "declare @result varchar(max) declare @n int,@lng bigint declare @xml_table table(xmlcol xml) insert into @xml_table(xmlcol) exec(\"" + sql.Replace("\"", "\"\"") + "\") select @result = cast(xmlcol as varchar(max)) from @xml_table  select @n = 1,@lng = len(@result) while (@n - 1) * 450000 <= @lng begin \tselect  substring(@result, (@n - 1) * 450000 + 1,  \t\tcase when @n * 450000 > @lng then @lng - (@n - 1) * 450000 else 450000 end) \tset @n = @n + 1 end ";
			FileStream ff = new FileStream(filename, FileMode.Append);
			BinaryWriter bw = new BinaryWriter(ff, Encoding.GetEncoding("gb2312"));
			bw.BaseStream.Seek(0L, SeekOrigin.End);
			bw.Write(Encoding.GetEncoding("gb2312").GetBytes(sql));
			bw.Flush();
			bw.Close();
			ff.Close();
			Process CmdPrc = new Process();
			CmdPrc.StartInfo.FileName = Hydeews.currentdir + "sqlcmd.exe";
			string filebegin = this.GetTmpFile();
			string fileend = this.GetTmpFile();
			if (File.Exists(filebegin))
			{
				File.Delete(filebegin);
			}
			if (File.Exists(fileend))
			{
				File.Delete(fileend);
			}
			FileStream ffilebegin = new FileStream(filebegin, FileMode.Append);
			StreamWriter swbegin = new StreamWriter(ffilebegin, Encoding.GetEncoding("gb2312"));
			swbegin.BaseStream.Seek(0L, SeekOrigin.End);
			swbegin.WriteLine("set nocount on ");
			swbegin.Flush();
			swbegin.Close();
			FileStream ffileend = new FileStream(fileend, FileMode.Append);
			StreamWriter swend = new StreamWriter(ffileend, Encoding.GetEncoding("gb2312"));
			swend.BaseStream.Seek(0L, SeekOrigin.End);
			swend.WriteLine("select '<$0>'");
			swend.Flush();
			swend.Close();
			CmdPrc.StartInfo.Arguments = string.Concat(new string[]
			{
				" -k1 -i\"",
				filebegin,
				"\",\"",
				filename,
				"\",\"",
				fileend,
				"\" -S\"",
				Hydeews.servername,
				"\" -U\"",
				Hydeews.logid,
				"\" -P\"",
				Hydeews.logpass,
				"\" -d\"",
				Hydeews.database,
				"\" -l5 -h-1 -o\"",
				objfile,
				"\" -t",
				timeout.ToString(),
				" -cgo -b -y0 "
			});
			CmdPrc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			CmdPrc.StartInfo.CreateNoWindow = true;
			CmdPrc.StartInfo.UseShellExecute = false;
			CmdPrc.StartInfo.RedirectStandardOutput = true;
			CmdPrc.Start();
			int waitsecs;
			if (timeout == 0)
			{
				waitsecs = 3600000;
			}
			else
			{
				waitsecs = timeout * 1000 + 1800000;
			}
			CmdPrc.WaitForExit(waitsecs);
			if (!CmdPrc.HasExited)
			{
				CmdPrc.Kill();
			}
			CmdPrc.Close();
			if (File.Exists(filebegin))
			{
				File.Delete(filebegin);
			}
			if (File.Exists(fileend))
			{
				File.Delete(fileend);
			}
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			int result;
			if (!File.Exists(objfile))
			{
				error = "执行语句未能收到返回结果！";
				result = -1;
			}
			else
			{
				FileStream fsread = new FileStream(objfile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
				if (fsread.Length < 5L)
				{
					fsread.Close();
					sr.Close();
					error = "执行语句失败。未收到有效的错误信息。";
					result = -1;
				}
				else if (fsread.Length > (long)maxsize && maxsize > 0)
				{
					fsread.Close();
					sr.Close();
					error = "结果超过" + maxsize.ToString() + "的长度限制。";
					if (File.Exists(objfile))
					{
						File.Delete(objfile);
					}
					result = -1;
				}
				else
				{
					byte[] data = new byte[4];
					fsread.Seek((long)(Convert.ToInt32(fsread.Length) - 6), SeekOrigin.Begin);
					fsread.Read(data, 0, 4);
					if (Encoding.GetEncoding("gb2312").GetString(data) != "<$0>")
					{
						byte[] err = new byte[fsread.Length];
						fsread.Seek(0L, SeekOrigin.Begin);
						fsread.Read(err, 0, Convert.ToInt32(fsread.Length));
						fsread.Close();
						sr.Close();
						File.Delete(objfile);
						error = Encoding.GetEncoding("gb2312").GetString(err);
						result = -1;
					}
					else
					{
						fsread.SetLength(fsread.Length - 6L);
						fsread.Flush();
						fsread.Close();
						sr.Close();
						string file_unioned = this.GetTmpFile();
						if (File.Exists(file_unioned))
						{
							File.Delete(file_unioned);
						}
						if (this.ExecCmdXml_Union(objfile, file_unioned, true) != 1)
						{
							error = "联接字符串失败。";
							result = -1;
						}
						else
						{
							outfile = file_unioned;
							result = 1;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000033E8 File Offset: 0x000015E8
		private int ExecCmdXml_Union(string srcfilename, string objfilename, bool deletesrcfile)
		{
			FileStream fsread = new FileStream(srcfilename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			FileStream fwrite = new FileStream(objfilename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
			StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
			StreamWriter sw = new StreamWriter(fwrite, Encoding.GetEncoding("gb2312"));
			while (!sr.EndOfStream)
			{
				sw.Write(sr.ReadLine());
			}
			fsread.Close();
			sr.Close();
			if (deletesrcfile)
			{
				File.Delete(srcfilename);
			}
			sw.Flush();
			fwrite.Flush();
			fwrite.Close();
			return 1;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x0000348C File Offset: 0x0000168C
		private int ExecCmdByFile(string filename, int timeout, int maxsize, out string outfile, out string error)
		{
			error = "";
			outfile = "";
			this.ResetConnectionsFromSettings();
			string objfile = this.GetTmpFile();
			Process CmdPrc = new Process();
			CmdPrc.StartInfo.FileName = Hydeews.currentdir + "sqlcmd.exe";
			string filebegin = this.GetTmpFile();
			string fileend = this.GetTmpFile();
			if (File.Exists(filebegin))
			{
				File.Delete(filebegin);
			}
			if (File.Exists(fileend))
			{
				File.Delete(fileend);
			}
			FileStream ffilebegin = new FileStream(filebegin, FileMode.Append);
			StreamWriter swbegin = new StreamWriter(ffilebegin, Encoding.GetEncoding("gb2312"));
			swbegin.BaseStream.Seek(0L, SeekOrigin.End);
			swbegin.WriteLine("set nocount on ");
			swbegin.Flush();
			swbegin.Close();
			FileStream ffileend = new FileStream(fileend, FileMode.Append);
			StreamWriter swend = new StreamWriter(ffileend, Encoding.GetEncoding("gb2312"));
			swend.BaseStream.Seek(0L, SeekOrigin.End);
			swend.WriteLine("select '<$0>'");
			swend.Flush();
			swend.Close();
			CmdPrc.StartInfo.Arguments = string.Concat(new string[]
			{
				" -k1 -i\"",
				filebegin,
				"\",\"",
				filename,
				"\",\"",
				fileend,
				"\" -S\"",
				Hydeews.servername,
				"\" -U\"",
				Hydeews.logid,
				"\" -P\"",
				Hydeews.logpass,
				"\" -d\"",
				Hydeews.database,
				"\" -l5 -h-1 -o\"",
				objfile,
				"\" -t",
				timeout.ToString(),
				" -cgo -b -W -s\"\t\" "
			});
			CmdPrc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			CmdPrc.StartInfo.CreateNoWindow = true;
			CmdPrc.StartInfo.UseShellExecute = false;
			CmdPrc.StartInfo.RedirectStandardOutput = true;
			CmdPrc.Start();
			int waitsecs;
			if (timeout == 0)
			{
				waitsecs = 3600000;
			}
			else
			{
				waitsecs = timeout * 1000 + 1800000;
			}
			CmdPrc.WaitForExit(waitsecs);
			if (!CmdPrc.HasExited)
			{
				CmdPrc.Kill();
			}
			CmdPrc.Close();
			if (File.Exists(filebegin))
			{
				File.Delete(filebegin);
			}
			if (File.Exists(fileend))
			{
				File.Delete(fileend);
			}
			if (File.Exists(filename))
			{
				File.Delete(filename);
			}
			int result;
			if (!File.Exists(objfile))
			{
				error = "执行语句未能收到返回结果！";
				result = -1;
			}
			else
			{
				int i = 1;
				while (this.IsFileOpened(objfile))
				{
					if (i > 10)
					{
						error = "打开文件" + objfile + "失败（超时）";
						return -1;
					}
					i++;
					Thread.Sleep(100);
				}
				FileStream fsread = new FileStream(objfile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
				if (fsread.Length < 5L)
				{
					fsread.Close();
					sr.Close();
					error = "执行语句失败。未收到有效的错误信息。";
					result = -1;
				}
				else
				{
					byte[] data = new byte[4];
					fsread.Seek((long)(Convert.ToInt32(fsread.Length) - 6), SeekOrigin.Begin);
					fsread.Read(data, 0, 4);
					if (Encoding.GetEncoding("gb2312").GetString(data) != "<$0>")
					{
						byte[] err = new byte[fsread.Length];
						fsread.Seek(0L, SeekOrigin.Begin);
						fsread.Read(err, 0, Convert.ToInt32(fsread.Length));
						fsread.Close();
						sr.Close();
						File.Delete(objfile);
						error = Encoding.GetEncoding("gb2312").GetString(err);
						result = -1;
					}
					else
					{
						fsread.SetLength(fsread.Length - 6L);
						fsread.Flush();
						fsread.Close();
						sr.Close();
						string file_replacenull = this.GetTmpFile();
						if (File.Exists(file_replacenull))
						{
							File.Delete(file_replacenull);
						}
						if (this.ExecCmd_ReplaceNull(objfile, file_replacenull, true) != 1)
						{
							error = "处理空值失败。";
							result = -1;
						}
						else
						{
							outfile = file_replacenull;
							result = 1;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x0000392C File Offset: 0x00001B2C
		private int ExecCmdByString(string sql, int timeout, int maxsize, out string outfile, out string error)
		{
			error = "";
			outfile = "";
			this.ResetConnectionsFromSettings();
			string objfile = this.GetTmpFile();
			Process CmdPrc = new Process();
			CmdPrc.StartInfo.FileName = Hydeews.currentdir + "sqlcmd.exe";
			CmdPrc.StartInfo.Arguments = string.Concat(new string[]
			{
				" -k1 -Q \" set nocount on ",
				sql.Replace("\"", "\"\""),
				"\t\n go \t\n select '<$0>' \" -S\"",
				Hydeews.servername,
				"\" -U\"",
				Hydeews.logid,
				"\" -P\"",
				Hydeews.logpass,
				"\" -d\"",
				Hydeews.database,
				"\" -l5 -h-1 -o\"",
				objfile,
				"\" -t",
				timeout.ToString(),
				" -cgo -b -W -s\"\t\" "
			});
			CmdPrc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			CmdPrc.StartInfo.CreateNoWindow = true;
			CmdPrc.StartInfo.UseShellExecute = false;
			CmdPrc.StartInfo.RedirectStandardOutput = true;
			CmdPrc.Start();
			int waitsecs;
			if (timeout == 0)
			{
				waitsecs = 3600000;
			}
			else
			{
				waitsecs = timeout * 1000 + 1800000;
			}
			CmdPrc.WaitForExit(waitsecs);
			if (!CmdPrc.HasExited)
			{
				CmdPrc.Kill();
			}
			CmdPrc.Close();
			int result;
			if (!File.Exists(objfile))
			{
				error = "执行语句未能收到返回结果";
				result = -1;
			}
			else
			{
				int i = 1;
				while (this.IsFileOpened(objfile))
				{
					if (i > 10)
					{
						error = "打开文件" + objfile + "失败（超时）";
						return -1;
					}
					i++;
					Thread.Sleep(100);
				}
				FileStream fsread = new FileStream(objfile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
				if (fsread.Length < 5L)
				{
					fsread.Close();
					sr.Close();
					error = "执行语句失败。未收到有效的错误信息。";
					result = -1;
				}
				else
				{
					byte[] data = new byte[4];
					fsread.Seek((long)(Convert.ToInt32(fsread.Length) - 6), SeekOrigin.Begin);
					fsread.Read(data, 0, 4);
					if (Encoding.GetEncoding("gb2312").GetString(data) != "<$0>")
					{
						byte[] err = new byte[fsread.Length];
						fsread.Seek(0L, SeekOrigin.Begin);
						fsread.Read(err, 0, Convert.ToInt32(fsread.Length));
						fsread.Close();
						sr.Close();
						File.Delete(objfile);
						error = Encoding.GetEncoding("gb2312").GetString(err);
						result = -1;
					}
					else
					{
						fsread.SetLength(fsread.Length - 6L);
						fsread.Flush();
						fsread.Close();
						sr.Close();
						string file_replacenull = this.GetTmpFile();
						if (File.Exists(file_replacenull))
						{
							File.Delete(file_replacenull);
						}
						if (this.ExecCmd_ReplaceNull(objfile, file_replacenull, true) != 1)
						{
							error = "替换空值失败！";
							result = -1;
						}
						else
						{
							outfile = file_replacenull;
							result = 1;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x00003C94 File Offset: 0x00001E94
		private int ExecCmd_ReplaceNull(string srcfilename, string objfilename, bool deletesrcfile)
		{
			try
			{
				FileStream fsread = new FileStream(srcfilename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				FileStream fwrite = new FileStream(objfilename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
				StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
				StreamWriter sw = new StreamWriter(fwrite, Encoding.GetEncoding("gb2312"));
				while (!sr.EndOfStream)
				{
					sw.WriteLine(("\t" + sr.ReadLine()).Replace("\tNULL", "\t").Substring(1));
				}
				fsread.Close();
				sr.Close();
				if (deletesrcfile)
				{
					File.Delete(srcfilename);
				}
				sw.Flush();
				fwrite.Flush();
				fwrite.Close();
			}
			catch
			{
				return -1;
			}
			return 1;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00003D74 File Offset: 0x00001F74
		private int ExecSimple(string sql, int timeout, int maxsize, out string result, out string error)
		{
			SqlConnection conn = null;
			DataSet ds = null;
			result = "";
			error = "";
			StringBuilder sbresult = new StringBuilder("");
			try
			{
				conn = new SqlConnection(Hydeews.hcs);
				conn.Open();
				SqlDataAdapter dda = new SqlDataAdapter();
				dda.SelectCommand = new SqlCommand
				{
					CommandText = " set quoted_identifier off " + sql + " if @@rowcount = 0 select ' '",
					CommandType = CommandType.Text,
					CommandTimeout = timeout,
					Connection = conn
				};
				ds = new DataSet();
				dda.Fill(ds, "row");
			}
			catch (Exception ex)
			{
				error = ex.Message.ToString();
				return -1;
			}
			finally
			{
				if (conn.State == ConnectionState.Open)
				{
					conn.Close();
				}
				conn = null;
			}
			for (int i = 0; i < ds.Tables["row"].Rows.Count; i++)
			{
				for (int j = 0; j < ds.Tables["row"].Columns.Count; j++)
				{
					sbresult.Append(ds.Tables["row"].Rows[i][j].ToString().Replace("\r", " ").Replace("\t", " ").Replace("\n", " ") + "\t");
				}
				sbresult.Append("\r");
				if (sbresult.Length > maxsize && maxsize > 0)
				{
					error = "结果超过" + maxsize.ToString() + "的长度限制。";
					return -1;
				}
			}
			if (sbresult.Length > 2)
			{
				sbresult.Remove(sbresult.Length - 2, 2);
			}
			result = sbresult.ToString();
			return 1;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00003FBC File Offset: 0x000021BC
		private bool checkverify(string verify_client)
		{
			if (Hydeews.verify_host == "UnLoad(Null)")
			{
				string tmp;
				int result = this.getverify(out tmp);
				if (result != 1)
				{
					return false;
				}
				Hydeews.verify_host = tmp;
			}
			return Hydeews.verify_host == verify_client;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00004020 File Offset: 0x00002220
		[WebMethod]
		public bool userlogon(string verify_client, string userid, string userpass, out string error)
		{
			bool result;
			if (!this.checkverify(verify_client))
			{
				error = "核对校验码失败。";
				result = false;
			}
			else if (!this.checkuser(userid, userpass))
			{
				error = "用户名密码错误。";
				result = false;
			}
			else
			{
				error = "";
				result = true;
			}
			return result;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000406C File Offset: 0x0000226C
		private int getverify(out string verifystr)
		{
			this.ClearTmpFile();
			SqlConnection conn = null;
			try
			{
				conn = new SqlConnection(Hydeews.hcs);
				conn.Open();
				SqlDataReader dr = new SqlCommand("select para from c_sys_ini (nolock) where ini = '2763'", conn)
				{
					CommandTimeout = 5
				}.ExecuteReader();
				if (!dr.Read())
				{
					verifystr = "检索参数失败。";
					return -1;
				}
				verifystr = dr.GetString(0);
			}
			catch (Exception ex)
			{
				verifystr = ex.Message.ToString();
				return -1;
			}
			finally
			{
				if (conn.State == ConnectionState.Open)
				{
					conn.Close();
				}
				conn = null;
			}
			return 1;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00004140 File Offset: 0x00002340
		private bool checkuser(string userid, string userpass)
		{
			SqlConnection conn = null;
			string tmp = null;
			try
			{
				conn = new SqlConnection(Hydeews.hcs);
				conn.Open();
				SqlDataReader dr = new SqlCommand(string.Concat(new string[]
				{
					"select top 1 userid from c_user (nolock) where userid='",
					userid.Replace("'", "''"),
					"' and userpass='",
					userpass.Replace("'", "''"),
					"'"
				}), conn)
				{
					CommandTimeout = 5
				}.ExecuteReader();
				if (!dr.Read())
				{
					return false;
				}
				tmp = dr.GetString(0);
			}
			catch
			{
				return false;
			}
			finally
			{
				if (conn.State == ConnectionState.Open)
				{
					conn.Close();
				}
				conn = null;
			}
			return tmp.ToLower().Trim() == userid.ToLower().Trim();
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000426C File Offset: 0x0000246C
		private bool IsFileOpened(string filename)
		{
			bool result = false;
			try
			{
				FileStream fs = File.OpenWrite(filename);
				fs.Close();
			}
			catch
			{
				result = true;
			}
			return result;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000042AC File Offset: 0x000024AC
		private string Reverse(string original)
		{
			char[] arr = original.ToCharArray();
			Array.Reverse(arr);
			return new string(arr);
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000042D4 File Offset: 0x000024D4
		private bool ResetConnectionsFromSettings()
		{
			bool result;
			try
			{
				if (Hydeews.servername == "UnLoad(Null)")
				{
					string tmp = Hydeews.hcs.Substring(Hydeews.hcs.IndexOf("data source=") + 12) + ";";
					Hydeews.servername = tmp.Substring(0, tmp.IndexOf(";"));
					tmp = Hydeews.hcs.Substring(Hydeews.hcs.IndexOf("user id=") + 8) + ";";
					Hydeews.logid = tmp.Substring(0, tmp.IndexOf(";"));
					tmp = Hydeews.hcs.Substring(Hydeews.hcs.IndexOf("password=") + 9) + ";";
					Hydeews.logpass = tmp.Substring(0, tmp.IndexOf(";"));
					tmp = Hydeews.hcs.Substring(Hydeews.hcs.IndexOf("initial catalog=") + 16) + ";";
					Hydeews.database = tmp.Substring(0, tmp.IndexOf(";"));
				}
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00004420 File Offset: 0x00002620
		private int SaveBytesToFile(byte[] bytes, string filename)
		{
			int result;
			try
			{
				FileStream fs = new FileStream(filename, FileMode.Append);
				BinaryWriter bWriter = new BinaryWriter(fs, Encoding.GetEncoding("gb2312"));
				bWriter.BaseStream.Seek(0L, SeekOrigin.End);
				bWriter.Write(bytes);
				bWriter.Flush();
				bWriter.Close();
				fs.Close();
				fs.Dispose();
				bytes = null;
				result = 1;
			}
			catch
			{
				result = -1;
			}
			return result;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000449C File Offset: 0x0000269C
		private int MakecabFile(string srcfile, string objfile, bool deletesrcfile)
		{
			int result;
			try
			{
				string objfile_simple = this.Reverse(objfile);
				objfile_simple = objfile_simple.Substring(0, objfile_simple.IndexOf("\\"));
				objfile_simple = this.Reverse(objfile_simple);
				string batfile = objfile + ".bat";
				if (File.Exists(objfile))
				{
					File.Delete(objfile);
				}
				if (File.Exists(objfile + "1"))
				{
					File.Delete(objfile + "1");
				}
				if (File.Exists(batfile))
				{
					File.Delete(batfile);
				}
				FileStream bat = new FileStream(batfile, FileMode.Append);
				StreamWriter swbat = new StreamWriter(bat, Encoding.GetEncoding("gb2312"));
				swbat.BaseStream.Seek(0L, SeekOrigin.End);
				swbat.WriteLine("path " + Hydeews.currentdir);
				swbat.WriteLine(string.Concat(new string[]
				{
					"hdzip.dll a -y -ep -m2 \"",
					objfile.Replace("\\", "\"\\\\\""),
					"1\" \"",
					srcfile.Replace("\\", "\"\\\\\""),
					"\""
				}));
				swbat.WriteLine(string.Concat(new string[]
				{
					"rename \"",
					objfile.Replace("\\", "\"\\\\\""),
					"1\" \"",
					objfile_simple,
					"\""
				}));
				if (deletesrcfile)
				{
					swbat.WriteLine("del \"" + srcfile.Replace("\\", "\"\\\"") + "\"");
				}
				swbat.WriteLine("del \"" + batfile.Replace("\\", "\"\\\"") + "\"");
				swbat.WriteLine("exit");
				swbat.Flush();
				bat.Close();
				Process CmdPrc = new Process();
				CmdPrc.StartInfo.FileName = batfile;
				string CmdArgments = "";
				CmdPrc.StartInfo.Arguments = CmdArgments;
				CmdPrc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
				CmdPrc.StartInfo.CreateNoWindow = true;
				CmdPrc.StartInfo.UseShellExecute = false;
				CmdPrc.StartInfo.RedirectStandardOutput = true;
				CmdPrc.Start();
				CmdPrc.WaitForExit(120000);
				if (!CmdPrc.HasExited)
				{
					CmdPrc.Kill();
				}
				CmdPrc.Close();
				if (!File.Exists(objfile))
				{
					Thread.Sleep(100);
				}
				if (File.Exists(batfile))
				{
					File.Delete(batfile);
				}
				if (File.Exists(srcfile) && deletesrcfile)
				{
					File.Delete(srcfile);
				}
				if (File.Exists(objfile + "1"))
				{
					File.Delete(objfile + "1");
				}
				if (!File.Exists(objfile))
				{
					result = -1;
				}
				else
				{
					int i = 0;
					while (this.IsFileOpened(objfile))
					{
						if (i > 10)
						{
							return -1;
						}
						Thread.Sleep(100);
						i++;
					}
					FileInfo fi = new FileInfo(objfile);
					if (fi.Length < 5L)
					{
						Thread.Sleep(1000);
					}
					fi = new FileInfo(objfile);
					if (fi.Length < 5L)
					{
						result = -1;
					}
					else
					{
						result = 1;
					}
				}
			}
			catch
			{
				result = -1;
			}
			return result;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x0000484C File Offset: 0x00002A4C
		private int ExpandFile(string srcfile, string objfile, bool deletesrcfile)
		{
			int result;
			try
			{
				string tmppath = this.Reverse(objfile);
				tmppath = this.Reverse(tmppath.Substring(tmppath.IndexOf("\\")));
				string objfile_simple = this.Reverse(objfile);
				objfile_simple = this.Reverse(objfile_simple.Substring(0, objfile_simple.IndexOf("\\")));
				string batfile = objfile + ".bat";
				if (File.Exists(batfile))
				{
					File.Delete(batfile);
				}
				if (File.Exists(objfile))
				{
					File.Delete(objfile);
				}
				if (File.Exists(objfile + "1"))
				{
					File.Delete(objfile + "1");
				}
				FileStream bat = new FileStream(batfile, FileMode.Append);
				StreamWriter swbat = new StreamWriter(bat, Encoding.GetEncoding("gb2312"));
				swbat.BaseStream.Seek(0L, SeekOrigin.End);
				swbat.WriteLine("path " + Hydeews.currentdir);
				swbat.WriteLine(tmppath.Substring(0, 2));
				swbat.WriteLine("cd \"" + tmppath + "\"");
				swbat.WriteLine(string.Concat(new string[]
				{
					"hdzip.dll rn -y \"",
					srcfile,
					"\" *.* \"",
					objfile_simple,
					"1\""
				}));
				swbat.WriteLine(string.Concat(new string[]
				{
					"hdzip.dll e  \"",
					srcfile.Replace("\\", "\"\\\""),
					"\" \"",
					tmppath,
					"\" -y"
				}));
				swbat.WriteLine(string.Concat(new string[]
				{
					"rename \"",
					objfile.Replace("\\", "\"\\\\\""),
					"1\" \"",
					objfile_simple,
					"\""
				}));
				if (deletesrcfile)
				{
					swbat.WriteLine("del \"" + srcfile.Replace("\\", "\"\\\"") + "\"");
				}
				swbat.WriteLine("del \"" + batfile.Replace("\\", "\"\\\"") + "\"");
				swbat.WriteLine("exit");
				swbat.Flush();
				bat.Close();
				Process CmdPrc = new Process();
				CmdPrc.StartInfo.FileName = batfile;
				string CmdArgments = "";
				CmdPrc.StartInfo.Arguments = CmdArgments;
				CmdPrc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
				CmdPrc.StartInfo.CreateNoWindow = true;
				CmdPrc.StartInfo.UseShellExecute = false;
				CmdPrc.StartInfo.RedirectStandardOutput = true;
				CmdPrc.Start();
				CmdPrc.WaitForExit(120000);
				if (!CmdPrc.HasExited)
				{
					CmdPrc.Kill();
				}
				CmdPrc.Close();
				if (!File.Exists(objfile))
				{
					Thread.Sleep(1000);
				}
				if (File.Exists(batfile))
				{
					File.Delete(batfile);
				}
				if (File.Exists(srcfile) && deletesrcfile)
				{
					File.Delete(srcfile);
				}
				if (File.Exists(objfile + "1"))
				{
					File.Delete(objfile + "1");
				}
				if (!File.Exists(objfile))
				{
					result = -1;
				}
				else
				{
					int i = 0;
					while (this.IsFileOpened(objfile))
					{
						if (i > 100)
						{
							return -1;
						}
						Thread.Sleep(10);
						i++;
					}
					result = 1;
				}
			}
			catch
			{
				result = -1;
			}
			return result;
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00004C2C File Offset: 0x00002E2C
		private string GetTmpFile()
		{
			return Path.GetTempFileName();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00004C44 File Offset: 0x00002E44
		private byte[] GetBytesFromFile(string filename)
		{
			byte[] result;
			try
			{
				FileStream fsread = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				StreamReader sr = new StreamReader(fsread, Encoding.GetEncoding("gb2312"));
				byte[] data = new byte[fsread.Length];
				int offset = 0;
				int size = Convert.ToInt32(fsread.Length);
				int remaining = data.Length;
				while (offset < data.Length)
				{
					if (remaining < size)
					{
						size = remaining;
					}
					int read = fsread.Read(data, offset, size);
					remaining -= read;
					offset += read;
				}
				fsread.Close();
				sr.Close();
				result = data;
			}
			catch
			{
				byte[] data = new byte[0];
				data = null;
				result = data;
			}
			return result;
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00004D04 File Offset: 0x00002F04
		[WebMethod]
		public int getstructure(string verify_client, string sql, int timeout, out string structure, out string error)
		{
			int result2;
			if (!this.checkverify(verify_client))
			{
				error = "校验码出错。";
				structure = "";
				result2 = -1;
			}
			else
			{
				SqlConnection conn = null;
				string result = "";
				try
				{
					conn = new SqlConnection(Hydeews.hcs);
					conn.Open();
					SqlDataAdapter dda = new SqlDataAdapter();
					dda.SelectCommand = new SqlCommand
					{
						CommandText = sql,
						CommandType = CommandType.Text,
						CommandTimeout = timeout,
						Connection = conn
					};
					DataSet ds = new DataSet();
					dda.Fill(ds, "tbl");
					for (int i = 0; i < ds.Tables["tbl"].Columns.Count; i++)
					{
						result = result + "<row columnname=\"" + ds.Tables["tbl"].Columns[i].ColumnName + "\" ";
						result = result + " columntype=\"" + ds.Tables["tbl"].Columns[i].DataType.ToString() + "\" />";
					}
				}
				catch (Exception ex)
				{
					error = ex.Message.ToString();
					structure = "";
					return -1;
				}
				finally
				{
					if (conn.State == ConnectionState.Open)
					{
						conn.Close();
					}
					conn = null;
				}
				error = "";
				structure = "<root>" + result + "</root>";
				result2 = 1;
			}
			return result2;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00004EE4 File Offset: 0x000030E4
		private int DistributeArray()
		{
			int result;
			try
			{
				Hydeews.mt.WaitOne();
				int ret = -1;
				for (int i = 0; i < Hydeews.storage_using.Length; i++)
				{
					if (!Hydeews.storage_using[i] || DateTime.Now.Subtract(Hydeews.storage_lasttime[i]).Minutes > 60)
					{
						if (Hydeews.storage_filelist[i] != null && !(Hydeews.storage_filelist[i] == ""))
						{
							if (File.Exists(Hydeews.storage_filelist[i]))
							{
								if (!this.IsFileOpened(Hydeews.storage_filelist[i]))
								{
									File.Delete(Hydeews.storage_filelist[i]);
								}
							}
						}
						Hydeews.storage_lasttime[i] = DateTime.Now;
						Hydeews.storage_filelist[i] = "";
						Hydeews.storage_using[i] = true;
						Hydeews.storage_pwd[i] = Guid.NewGuid().ToString().ToLower().Replace("-", "").Substring(4, 16);
						ret = i;
						break;
					}
				}
				if (ret >= Hydeews.storage_distribed)
				{
					Hydeews.storage_distribed = ret + 1;
				}
				Hydeews.mt.ReleaseMutex();
				result = ret;
			}
			catch
			{
				result = -1;
			}
			return result;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x0000506C File Offset: 0x0000326C
		private void ClearTmpFile()
		{
			DirectoryInfo dir = new DirectoryInfo(Path.GetTempPath());
			FileInfo[] files = dir.GetFiles("*.*");
			for (int i = 0; i < files.Length; i++)
			{
				if (DateTime.Now.Subtract(files[i].LastWriteTime).Days > 1)
				{
					try
					{
						File.Delete(files[i].FullName);
					}
					catch
					{
					}
				}
			}
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00005100 File Offset: 0x00003300
		[WebMethod]
		public string getfilelist(string verify_client)
		{
			string result;
			try
			{
				if (!this.checkverify(verify_client))
				{
					result = "";
				}
				else
				{
					string filelist = "";
					DirectoryInfo dir = new DirectoryInfo(Hydeews.currentdir + "download");
					FileInfo[] files = dir.GetFiles("*.*");
					for (int i = 0; i < files.Length; i++)
					{
						filelist = filelist + files[i].Name + ";";
					}
					result = filelist;
				}
			}
			catch
			{
				result = "";
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x0000519C File Offset: 0x0000339C
		[WebMethod]
		public int getfileinfo(string verify_client, string filename, out int filesize, out string filetime, out string error)
		{
			filesize = 0;
			filetime = DateTime.Today.ToString();
			error = "";
			int result;
			try
			{
				if (!this.checkverify(verify_client))
				{
					error = "核对验证码失败。";
					result = -1;
				}
				else if (!File.Exists(Hydeews.currentdir + "download\\" + filename))
				{
					error = "无此文件。";
					result = -1;
				}
				else
				{
					FileInfo fi = new FileInfo(Hydeews.currentdir + "download\\" + filename);
					filesize = (int)fi.Length;
					filetime = fi.LastWriteTime.ToString();
					result = 1;
				}
			}
			catch
			{
				error = "得到文件信息失败。";
				result = -1;
			}
			return result;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00005264 File Offset: 0x00003464
		[WebMethod]
		public int downloadfile(string verify_client, string filename, int compressionlevel, out byte[] filedata, out bool outiscompression, out string error)
		{
			filedata = Encoding.GetEncoding("gb2312").GetBytes("");
			outiscompression = false;
			error = "";
			int result;
			if (!this.checkverify(verify_client))
			{
				error = "核对验证码失败。";
				result = -1;
			}
			else if (!File.Exists(Hydeews.currentdir + "download\\" + filename))
			{
				error = "无此文件。";
				result = -1;
			}
			else
			{
				try
				{
					FileInfo fi = new FileInfo(Hydeews.currentdir + "download\\" + filename);
					if (fi.Length <= (long)compressionlevel || compressionlevel == 0)
					{
						filedata = this.GetBytesFromFile(Hydeews.currentdir + "download\\" + filename);
						if (filedata == null)
						{
							error = "读取文件失败！";
							filedata = Encoding.GetEncoding("gb2312").GetBytes("");
							result = -1;
						}
						else
						{
							result = 1;
						}
					}
					else
					{
						string objfile = this.GetTmpFile();
						if (this.MakecabFile(Hydeews.currentdir + "download\\" + filename, objfile, false) != 1)
						{
							error = "压缩文件失败。";
							result = -1;
						}
						else
						{
							int i = 0;
							while (this.IsFileOpened(objfile))
							{
								if (i > 100)
								{
									error = "打开文件失败。";
									return -1;
								}
								Thread.Sleep(10);
								i++;
							}
							filedata = this.GetBytesFromFile(objfile);
							if (filedata == null)
							{
								error = "打开文件失败@downloadfile";
								filedata = Encoding.GetEncoding("gb2312").GetBytes("");
								result = -1;
							}
							else
							{
								outiscompression = true;
								if (File.Exists(objfile))
								{
									File.Delete(objfile);
								}
								result = 1;
							}
						}
					}
				}
				catch
				{
					error = "下载文件失败。";
					result = -1;
				}
			}
			return result;
		}

		// Token: 0x04000001 RID: 1
		public static string currentdir = AppDomain.CurrentDomain.BaseDirectory;

		// Token: 0x04000002 RID: 2
		public static string verify_host = "UnLoad(Null)";

		// Token: 0x04000003 RID: 3
		public static string servername = "UnLoad(Null)";

		// Token: 0x04000004 RID: 4
		public static string database = "UnLoad(Null)";

		// Token: 0x04000005 RID: 5
		public static string logid = "UnLoad(Null)";

		// Token: 0x04000006 RID: 6
		public static string logpass = "UnLoad(Null)";

		// Token: 0x04000007 RID: 7
		public static string hcs = ConfigurationManager.AppSettings["hydeecons"];

		// Token: 0x04000008 RID: 8
		public static Mutex mt = new Mutex();

		// Token: 0x04000009 RID: 9
		public static string[] storage_filelist = new string[262144];

		// Token: 0x0400000A RID: 10
		public static bool[] storage_using = new bool[262144];

		// Token: 0x0400000B RID: 11
		public static DateTime[] storage_lasttime = new DateTime[262144];

		// Token: 0x0400000C RID: 12
		public static string[] storage_pwd = new string[262144];

		// Token: 0x0400000D RID: 13
		public static int storage_distribed = 0;

		// Token: 0x02000003 RID: 3
		public enum RequestTypeEnum
		{
			// Token: 0x0400000F RID: 15
			CmdFormat = 1,
			// Token: 0x04000010 RID: 16
			CmdXml,
			// Token: 0x04000011 RID: 17
			Simple = 10
		}
	}
}
