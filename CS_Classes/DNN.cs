﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace CS_Classes
{
    public class DNN
    {
        public void Run(string protoTxt, string caffeModel, string synsetWords, Mat image, bool AvoidDNNCrashes)
        {
            var classNames = File.ReadAllLines(synsetWords)
                .Select(line => line.Split(' ').Last())
                .ToArray();

            PrepareModel(caffeModel);
            Console.WriteLine(" Done");

            using (var net = CvDnn.ReadNetFromCaffe(protoTxt, caffeModel))
            {
                Console.WriteLine("Layer names: {0}", string.Join(", ", net.GetLayerNames()));
                Console.WriteLine();

                // Convert Mat to batch of images
                using (var inputBlob = CvDnn.BlobFromImage(image, 1, new Size(224, 224), new Scalar(104, 117, 123)))
                {
                    net.SetInput(inputBlob, "data");
                    if (AvoidDNNCrashes == false)
                    {
                        using (var prob = net.Forward("prob"))
                        {
                            // find the best class
                            GetMaxClass(prob, out int classId, out double classProb);
                            Console.WriteLine("Best class: #{0} '{1}'", classId, classNames[classId]);
                            Console.WriteLine("Probability: {0:P2}", classProb);

                            Console.WriteLine("Press any key to exit");
                            Console.Read();
                        }
                    }
                }
            }
        }

        private static byte[] DownloadBytes(string url)
        {
            var client = WebRequest.CreateHttp(url);
            using (var response = client.GetResponse())
            using (var responseStream = response.GetResponseStream())
            {
                using (var memory = new MemoryStream())
                {
                    responseStream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        private static void PrepareModel(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.Write("Downloading Caffe Model...");
                var contents = DownloadBytes("http://dl.caffe.berkeleyvision.org/bvlc_googlenet.caffemodel");
                File.WriteAllBytes(fileName, contents);
            }
        }

        /// <summary>
        /// Find best class for the blob (i. e. class with maximal probability)
        /// </summary>
        /// <param name="probBlob"></param>
        /// <param name="classId"></param>
        /// <param name="classProb"></param>
        private static void GetMaxClass(Mat probBlob, out int classId, out double classProb)
        {
            // reshape the blob to 1x1000 matrix
            using (var probMat = probBlob.Reshape(1, 1))
            {
                Cv2.MinMaxLoc(probMat, out _, out classProb, out _, out var classNumber);
                classId = classNumber.X;
            }
        }
    }
}