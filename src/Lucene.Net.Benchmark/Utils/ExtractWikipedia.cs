﻿using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Benchmarks.Utils
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Extract the downloaded Wikipedia dump into separate files for indexing.
    /// </summary>
    public class ExtractWikipedia
    {
        private DirectoryInfo outputDir;

        public static int count = 0;

        internal static readonly int BASE = 10;
        protected DocMaker m_docMaker;

        public ExtractWikipedia(DocMaker docMaker, DirectoryInfo outputDir)
        {
            this.outputDir = outputDir;
            this.m_docMaker = docMaker;
            SystemConsole.WriteLine("Deleting all files in " + outputDir);
            FileInfo[] files = outputDir.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                files[i].Delete();
            }
        }

        public virtual DirectoryInfo Directory(int count, DirectoryInfo directory)
        {
            if (directory == null)
            {
                directory = outputDir;
            }
            int @base = BASE;
            while (@base <= count)
            {
                @base *= BASE;
            }
            if (count < BASE)
            {
                return directory;
            }
            directory = new DirectoryInfo(System.IO.Path.Combine(directory.FullName, (((int)(@base / BASE)).ToString(CultureInfo.InvariantCulture))));
            directory = new DirectoryInfo(System.IO.Path.Combine(directory.FullName, (((int)(count / (@base / BASE))).ToString(CultureInfo.InvariantCulture))));
            return Directory(count % (@base / BASE), directory);
        }

        public virtual void Create(string id, string title, string time, string body)
        {
            DirectoryInfo d = Directory(count++, null);
            d.Create();
            FileInfo f = new FileInfo(System.IO.Path.Combine(d.FullName, id + ".txt"));

            StringBuilder contents = new StringBuilder();

            contents.Append(time);
            contents.Append("\n\n");
            contents.Append(title);
            contents.Append("\n\n");
            contents.Append(body);
            contents.Append("\n");

            try
            {
                using (TextWriter writer = new StreamWriter(new FileStream(f.FullName, FileMode.Create, FileAccess.Write), Encoding.UTF8))
                    writer.Write(contents.ToString());
            }
            catch (IOException ioe)
            {
                throw new Exception(ioe.ToString(), ioe);
            }
        }

        public virtual void Extract()
        {
            Document doc = null;
            SystemConsole.WriteLine("Starting Extraction");
            long start = Support.Time.CurrentTimeMilliseconds();
            try
            {
                while ((doc = m_docMaker.MakeDocument()) != null)
                {
                    Create(doc.Get(DocMaker.ID_FIELD), doc.Get(DocMaker.TITLE_FIELD), doc
                        .Get(DocMaker.DATE_FIELD), doc.Get(DocMaker.BODY_FIELD));
                }
            }
            catch (NoMoreDataException /*e*/)
            {
                //continue
            }
            long finish = Support.Time.CurrentTimeMilliseconds();
            SystemConsole.WriteLine("Extraction took " + (finish - start) + " ms");
        }

        public static void Main(string[] args)
        {

            FileInfo wikipedia = null;
            DirectoryInfo outputDir = new DirectoryInfo("./enwiki");
            bool keepImageOnlyDocs = true;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--input", StringComparison.Ordinal) || arg.Equals("-i", StringComparison.Ordinal))
                {
                    wikipedia = new FileInfo(args[i + 1]);
                    i++;
                }
                else if (arg.Equals("--output", StringComparison.Ordinal) || arg.Equals("-o", StringComparison.Ordinal))
                {
                    outputDir = new DirectoryInfo(args[i + 1]);
                    i++;
                }
                else if (arg.Equals("--discardImageOnlyDocs", StringComparison.Ordinal) || arg.Equals("-d", StringComparison.Ordinal))
                {
                    keepImageOnlyDocs = false;
                }
            }

            IDictionary<string, string> properties = new Dictionary<string, string>();
            properties["docs.file"] = wikipedia.FullName;
            properties["content.source.forever"] = "false";
            properties["keep.image.only.docs"] = keepImageOnlyDocs.ToString();
            Config config = new Config(properties);

            ContentSource source = new EnwikiContentSource();
            source.SetConfig(config);

            DocMaker docMaker = new DocMaker();
            docMaker.SetConfig(config, source);
            docMaker.ResetInputs();
            if (wikipedia.Exists)
            {
                SystemConsole.WriteLine("Extracting Wikipedia to: " + outputDir + " using EnwikiContentSource");
                outputDir.Create();
                ExtractWikipedia extractor = new ExtractWikipedia(docMaker, outputDir);
                extractor.Extract();
            }
            else
            {
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            SystemConsole.Error.WriteLine("Usage: java -cp <...> org.apache.lucene.benchmark.utils.ExtractWikipedia --input|-i <Path to Wikipedia XML file> " +
                    "[--output|-o <Output Path>] [--discardImageOnlyDocs|-d]");
            SystemConsole.Error.WriteLine("--discardImageOnlyDocs tells the extractor to skip Wiki docs that contain only images");
        }
    }
}
