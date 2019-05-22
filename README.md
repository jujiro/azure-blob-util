# azure-blob-util
A lightweight utility to list, copy, and delete blobs from Azure storage

## Introduction
Microsoft provides Azure Storage Explorer to perform various operations.  It is a great tool, however, it is still an interactive tool.  AzCopy is a yet another utility from Microsoft.  AzCopy is a batch utility and runs very fast.  Azure-blob-util is a hybrid of Azure Storage Explorer and AzCopy.

Using this command line tool, you can list, delete, download, or copy blobs.  The copy function copies blobs from one storage location to the other.

## Requirements
Windows platform with .Net 4.6.1 or above installed.

## Usage
If you just want to use the utility, download the distribution folder's contents.
Run abu.exe from the command or powershell prompt.

### Help
To get the command line options:  
**abu**  
**abu -?**

The utility works with pattern or patter files.  Use the pattern files where multiple patterns are involved.  The pattern file can be located in any folder.  Make sure to specify the fully qualified pattern file name if it is not in the current working folder.  
**Note that the Azure blobs are case sensitive.**

### List blobs 
Update the abu.exe.config file with the storage location you want to list.  
Update the two keys, **storageConnectionString** and **storageContainer**.  
Example:  
    <appSettings>
        <add key="storageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=shgdgdhhd77djjdjd99odkkddpuususususd9d9d9dsomelongkey==;" />
        <add key="storageContainer" value="mycontainer" />

The following commands will list all blobs in the specified container:   
**abu -l** or  
**abu -list**

The following commands will list all blobs in the specified container, which start with **temp**:   
**abu -l temp** or  
**abu -list temp**

Make a text file, mylist.txt and enter a few patterns.  
**temp**  
**chrome**

The following commands will list all blobs in the specified container, which start with **temp** and **chrome**:  
**abu -lf mylist.txt** or  
**abu -listfile mylist.txt**  

The following commands will list all blobs and their sizes in the specified container, which start with **temp** and **chrome**:  
**abu -lfs mylist.txt** or  
**abu -listfilesize mylist.txt**  

You can dump the blob listing to another file by redirecting the output:  
**abu -lf mylist.txt > myoutfile.txt**  
This is particularly useful when you want to delete some blobs.

### Delete blobs
Make sure the storage location is correct in the bu.exe.config file.  Delete blobs option works on specific blobs.  Make a list of blobs in some text file (mylist.txt) keeping one blob per line. 
**temp/blob1.txt**  
**chrome/folder1/blob2.data**  

To delete these blobs use one of the following commands:  
**abu -d mylist.txt** or  
**abu -delete mylist.txt**  

Delete blobs option does not work on patterns.  You must supply an explicit list of blobs.

### Download blobs
Make sure the storage location is correct in the bu.exe.config file.  To download blobs using pattern use the following commands:  
**abu -g temp** or  
**abu -get temp**  

A new guid folder would be created in the current working folder.  All the blobs starting with **temp** would be copied into the guid folder.  The blobs would be copied keeping the original folder structure in tact.

To download multi-pattern blobs, make some text file, myblobs.txt.  Enter the pattern or exact blob names per line.  
**temp**  
**chrome/folder1/blob2.data**  

Use the following commands to download blobs which start with **temp** and **chrome/folder1/blob2.data**.
**abu -g myblobs.txt** or  
**abu -get myblobs.txt**  

Notice that this syntax is similar to the pattern syntax.  The utility would use the file syntax if a file is found.  If your intent was to download all blobs starting with myblobs.txt, then ensure that this file does not exist.

You can download the blobs in just one folder making their names flat.  The slashes in the blob names would be converted to dashes.  Use the following commands using a pattern or file name:  
**abu -gf myblobs.txt** or  
**abu -getflat myblobs.txt**  

### Copy blobs
This feature copies blobs from one Azure storage location to the other.  Update the abu.exe.config file with the source and target storage locations.  
Update the four keys, **storageConnectionString**, **storageContainer**, **targetStorageConnectionString**, and **targetStorageContainer**.  
Example:  
    <appSettings>
        <add key="storageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=shgdgdhhd77djjdjd99odkkddpuususususd9d9d9dsomelongkey==;" />
        <add key="storageContainer" value="mycontainer" />
        <add key="targetStorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=mystorageaccount2;AccountKey=blahblahodkkddpuususususd9d9d9dsomelongkey==;" />
        <add key="targetStorageContainer" value="mycontainer2" />

Make a list of blobs in some text file (mylist.txt.)  use the following commands to copy blobs from source to target.  
**abu -c mylist.txt** or  
**abu -copy mylist.txt**  

