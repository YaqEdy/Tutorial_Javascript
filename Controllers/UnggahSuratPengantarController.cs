 [HttpPost]
        public JObject UploadSP()
        {
            var jreturn = new JObject();
            var iid = "";
            try
            {
                pUser = Session["username"].ToString();
                var iparams = Request["params"];
                var itype = Request["type"];
                Int64 ticket = 0;
                if (Session["ticket"] != null)
                {
                    Int64.TryParse(Session["ticket"].ToString(), out ticket);
                }
                else
                {
                    ticket = jk.createTicketUpload(pUser);
                    Session["ticket"] = ticket;
                }

                for (int i = 0; i < Request.Files.Count; i++)
                {
                    HttpPostedFileBase file = Request.Files[i];
                    int fileSize = file.ContentLength;
                    string fileName = $"{file.FileName.Replace(" ", String.Empty)}";
                    string mimeType = file.ContentType;
                    if (mimeType != "application/pdf")
                    {
                        throw new System.ArgumentException("Mohon upload file bertipe pdf.", "Peringatan!");
                    }
                    Stream fileContent = file.InputStream;
                    var dir = "C:\\tempSP\\";

                    var path = Path.Combine(dir, file.FileName);
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    file.SaveAs(path);

                    byte[] files;
                    //convert lokasi path file ke tipe byte[]
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(stream))
                        {
                            files = reader.ReadBytes((int)stream.Length);
                        }
                    }
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    var uploaddate = DateTime.Now;
                    var myConn = obj.myConn();
                    SqlCommand cmd = new SqlCommand("usp_saveSuratPengantar", myConn);
                    cmd.Parameters.AddWithValue("@fg", "0");
                    cmd.Parameters.AddWithValue("@filename", fileName);
                    cmd.Parameters.AddWithValue("@username", pUser);
                    cmd.Parameters.AddWithValue("@IDPelapor", Session["idpelapor"].ToString());
                    cmd.Parameters.AddWithValue("@uploaddate", uploaddate.ToString("yyyy-MM-dd hh:mm:ss:fff"));
                    cmd.Parameters.AddWithValue("@type", itype);
                    cmd.Parameters.AddWithValue("@fgstatus", "O");
                    DataTable datasource = obj.GetList(myConn, cmd);
                    myConn.Close();
                    iid = datasource.Rows[0]["id"].ToString();
                    var underlying = itype;
                    var result = jk.execUploadPost(ticket, Session["username"].ToString(), Session["idpelapor"].ToString(), fileName, mimeType, files, iparams, uploaddate, underlying);
                    var statusRI = Convert.ToBoolean(result.GetValue("statusRI"));
                    var isuccess = true;
                    var msg = "";
                    if (statusRI && Convert.ToBoolean(result.GetValue("success")))
                    {
                        isuccess = true;
                        msg = "Berhasil Upload.";
                        //finish upload
                        var myConn1 = obj.myConn();
                        SqlCommand cmd1 = new SqlCommand("usp_saveSuratPengantar", myConn1);
                        cmd1.Parameters.AddWithValue("@fg", "1");
                        cmd1.Parameters.AddWithValue("@id", iid);
                        cmd1.Parameters.AddWithValue("@FullPath", result.GetValue("FullPath").ToString());
                        cmd1.Parameters.AddWithValue("@uploadfinishdate", result.GetValue("jamfinish").ToString());
                        DataTable datasource1 = obj.GetList(myConn1, cmd1);
                        myConn1.Close();

                        //insert detail
                        var myConn2 = obj.myConn();
                        SqlCommand cmd2 = new SqlCommand("usp_saveSuratPengantar", myConn2);
                        cmd2.Parameters.AddWithValue("@fg", "2");
                        cmd2.Parameters.AddWithValue("@id", iid);
                        cmd2.Parameters.AddWithValue("@params", iparams);
                        DataTable datasource2 = obj.GetList(myConn2, cmd2);
                        myConn2.Close();

                        #region Send Email
                        //JObject jdt = obj.getPengumumanAllowFileJson();

                        var myConn3 = obj.myConn();
                        SqlCommand cmd3 = new SqlCommand("usp_getSP_Email", myConn3);
                        DataTable ds3 = obj.GetList(myConn3, cmd3);
                        myConn3.Close();

                        bool istatusEmail = false;
                        if (ds3.Rows.Count > 0)
                        {
                            //var icount=ds3.Rows.Count;
                            //for(int k=0;k< icount; k++)
                            //{
                            var toAddress = ds3.Rows[0]["to_email"].ToString();
                            var ccAddress = ds3.Rows[0]["cc_email"].ToString();
                            var content = ds3.Rows[0]["Content"].ToString();
                            var Subject = ds3.Rows[0]["Subject"].ToString();
                            //var toAddress = jdt.GetValue("SP_mail_to").ToString();
                            //var content = jdt.GetValue("SP_content").ToString();
                            //var Subject = "Unggah Surat Pengantar";
                            istatusEmail = mail.SendEmailSuratPengantar("U",toAddress, ccAddress, content, Subject);
                            //}
                        }
                        jreturn.Add("email", istatusEmail);
                        #endregion Send Email
                    }
                    else
                    {
                        //Delete
                        var myConn3 = obj.myConn();
                        SqlCommand cmd3 = new SqlCommand("usp_saveSuratPengantar", myConn3);
                        cmd3.Parameters.AddWithValue("@fg", "4");
                        cmd3.Parameters.AddWithValue("@id", iid);
                        DataTable datasource2 = obj.GetList(myConn3, cmd3);
                        myConn3.Close();

                        isuccess = false;
                        msg = "Gagal Upload.";
                        if (!statusRI)
                        {
                            jreturn.Add("statusRI", statusRI);
                            jreturn.Add("messageRI", result.GetValue("messageRI").ToString());
                        }
                    }


                    jreturn.Add("success", isuccess);
                    jreturn.Add("message", msg);


                    //var ischeck = InsertToDatabase(path, fileName, mimeType, ticket, pk);
                    //if (ischeck == true)
                    //{
                    //    jk.DeleteTempFile(path);
                    //    jreturn.Add("success", true);
                    //    jreturn.Add("message", "Berhasil Upload.");
                    //}
                    //else
                    //{
                    //    jreturn.Add("gagal",false);
                    //    jreturn.Add("message", "Gagal Upload.");
                    //}
                }

            }
            catch (Exception ex)
            {
                //Delete
                var myConn3 = obj.myConn();
                SqlCommand cmd3 = new SqlCommand("usp_saveSuratPengantar", myConn3);
                cmd3.Parameters.AddWithValue("@fg", "4");
                cmd3.Parameters.AddWithValue("@id", iid);
                DataTable datasource2 = obj.GetList(myConn3, cmd3);
                myConn3.Close();

                jreturn.Add("success", false);
                jreturn.Add("message", ex.Message);
            }
            return jreturn;

        }