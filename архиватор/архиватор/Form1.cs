using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace архиватор
{
    public partial class Form1 : Form
    {
        public class c_FileToArchive
        {
            public String SelectedFilePath { get; set; }
            public String SelectedFile { get; set; }
        }
        c_FileToArchive pc_leha = new c_FileToArchive();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            button2.Enabled = false;
            button3.Enabled = false;
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pc_leha.SelectedFilePath = openFileDialog1.FileName;
                pc_leha.SelectedFile = Path.GetFileName(openFileDialog1.FileName);
                textBox1.Text = Path.GetFileName(openFileDialog1.FileName);
                if (Path.GetExtension(pc_leha.SelectedFilePath) == ".size")
                {
                    button3.Enabled = true;
                }
                else { button2.Enabled = true; }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FileStream lfs_ReadFile = new FileStream(pc_leha.SelectedFilePath, FileMode.Open);
            BinaryReader lbr = new BinaryReader(lfs_ReadFile);
            Byte[] lba_inFA = lbr.ReadBytes((int)lfs_ReadFile.Length);
            
            lbr.Close();
            lfs_ReadFile.Close();

            List<Byte> list_EncryptFirst = new List<Byte>();
            Byte lb_Byte = 0;//дублирующий байт
            Int32 li_Count = 1; //колво дубл байтов
            //поиск дублирующих байтов
            for (int i = 0, j = 1; i < lba_inFA.Length; i++, j++)
            {
                if (j != lba_inFA.Length)
                {
                    //если найден дубл байт то увел число на 1
                    if (lba_inFA[i] == lba_inFA[j])
                    {
                        li_Count++;
                    }

                    //если след байт не дубл то запись в листтекст кол повторений и сам байт
                    if (lba_inFA[i] != lba_inFA[j])
                    {
                        lb_Byte = lba_inFA[i];
                        list_EncryptFirst.Add((Byte)li_Count);
                        list_EncryptFirst.Add(lb_Byte);
                        li_Count = 1;
                    }

                    //больше 256 байт нельзя поэтому если дубл больше 256 сокр до 255
                    if (li_Count == 255)
                    {
                        lb_Byte = lba_inFA[i];
                        list_EncryptFirst.Add((Byte)li_Count);
                        list_EncryptFirst.Add(lb_Byte);
                        li_Count = 0;
                    }
                }
                
                else
                {
                    lb_Byte = lba_inFA[i];
                    list_EncryptFirst.Add((Byte)li_Count);
                    list_EncryptFirst.Add(lb_Byte);
                    li_Count = 1;
                }
            }
            List<Byte> list_EncryptTwo = new List<Byte>();
            li_Count = 0;
            Int32 li_MarkFirst = 0;
            Boolean lb_FindMark = false;
            for (int i = 0; i < list_EncryptFirst.Count; i += 2)
            {
                if (list_EncryptFirst[i] == 1)
                {
                    if (lb_FindMark == false)
                    {
                        lb_FindMark = true;
                        li_MarkFirst = i + 1;
                        list_EncryptTwo.Add(00);
                    }

                    li_Count++;
                    //снова разделяем по 255
                    if (li_Count == 255)
                    {
                        list_EncryptTwo.Add((Byte)li_Count);
                        for (int x = 0; x < li_Count; x++)
                        {
                            list_EncryptTwo.Add(list_EncryptFirst[li_MarkFirst]);
                            li_MarkFirst += 2;
                        }
                        lb_FindMark = false;
                        li_Count = 0;
                    }
                }

                if (list_EncryptFirst[i] != 1)
                {
                    if (lb_FindMark == true)
                    {
                        list_EncryptTwo.Add((Byte)li_Count);
                        for (int x = 0; x < li_Count; x++)
                        {
                            list_EncryptTwo.Add(list_EncryptFirst[li_MarkFirst]);
                            li_MarkFirst += 2;
                        }
                        lb_FindMark = false;
                        li_Count = 0;
                        i -= 2;
                    }
                   
                    else
                    {
                        list_EncryptTwo.Add(list_EncryptFirst[i]);
                        list_EncryptTwo.Add(list_EncryptFirst[i + 1]);
                    }
                }

                if (i == list_EncryptFirst.Count - 2 & lb_FindMark == true)
                {
                    list_EncryptTwo.Add((Byte)li_Count);
                    for (int x = 0; x < li_Count; x++)
                    {
                        list_EncryptTwo.Add(list_EncryptFirst[li_MarkFirst]);
                        li_MarkFirst += 2;
                    }
                }
            }
            
            //добавим в начало массива 4 байт расширения файла
            Byte[] lba_Extension = Encoding.Default.GetBytes(Path.GetExtension(pc_leha.SelectedFile));
            list_EncryptTwo.InsertRange(0, lba_Extension);
           
            String ls_ArchiveFileName = Path.GetDirectoryName(pc_leha.SelectedFilePath) + "\\" + Path.GetFileNameWithoutExtension(pc_leha.SelectedFile) + ".size";
            FileStream lfs_WriteArchive = new FileStream(ls_ArchiveFileName, FileMode.Create);
            Byte[] ba_Archive = new Byte[list_EncryptTwo.Count];
            list_EncryptTwo.CopyTo(ba_Archive);
            lfs_WriteArchive.Write(ba_Archive, 0, ba_Archive.Length);
            lfs_WriteArchive.Close();
            button2.Enabled = false;
            textBox1.Text = "ВЫПОЛНЕНО";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FileStream lfs_ReadArchive = new FileStream(pc_leha.SelectedFilePath, FileMode.Open);
            BinaryReader lbr_ReadArchive = new BinaryReader(lfs_ReadArchive);
            //прочитаем сначала расширение файла
            Byte[] ba_Extension = lbr_ReadArchive.ReadBytes(4);
            //затем все остальное
            Byte[] ba_ArchivedFile = lbr_ReadArchive.ReadBytes((int)lfs_ReadArchive.Length);
            lbr_ReadArchive.Close();
            lfs_ReadArchive.Close();
            List<Byte> list_Extract = new List<Byte>();
            int m = 0;
            //цикл извлечения из архива
            while (m != ba_ArchivedFile.Length)
            {
                if (ba_ArchivedFile[m] == 00)
                {
                    Int32 j = m + 1;
                    for (int i = 1; i < (int)ba_ArchivedFile[j] + 1; i++)
                    {
                        list_Extract.Add(ba_ArchivedFile[j + i]);
                    }
                    m = m + (int)ba_ArchivedFile[j] + 2;
                }
                else
                {
                    for (int i = 0; i < (int)ba_ArchivedFile[m]; i++)
                    {
                        list_Extract.Add(ba_ArchivedFile[m + 1]);
                    }
                    m += 2;
                }
            }
            String ls_Extension = Encoding.Default.GetString(ba_Extension);
            String ls_ExtractFileName = Path.GetDirectoryName(pc_leha.SelectedFilePath) + "\\" + Path.GetFileNameWithoutExtension(pc_leha.SelectedFile) + ".output" + ls_Extension;
            FileStream lfs_WriteArchive = new FileStream(ls_ExtractFileName, FileMode.Create);
            Byte[] ba_Extract = new Byte[list_Extract.Count];
            list_Extract.CopyTo(ba_Extract);
            lfs_WriteArchive.Write(ba_Extract, 0, ba_Extract.Length);
            lfs_WriteArchive.Close();
            button3.Enabled = false;
            textBox1.Text = "ВЫПОЛНЕНО";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("как работать с программой" +
                "1)выберите файл с которым вы хотите работать" +
                "2)добавте его в архив" +
                "3)извлечь из архива");
        }
    }
}
