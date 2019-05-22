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

### List blobs 
Update the abu.exe.config file with the storage location you want to list.
Update the two keys, **storageConnectionString** and **storageContainer**.
Example:

    <appSettings>
        <add key="storageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=shgdgdhhd77djjdjd99odkkddpuususususd9d9d9dsomelongkey==;" />
        <add key="storageContainer" value="mycontainer" />

The following commands will list all blobs in the specified container:   
**abu -l**
**abu -list**

The following commands will list all blobs in the specified container, which start with **temp**:   
**abu -l temp**  
**abu -list temp**

Make a text file, mylist.txt and enter a few patterns.  
**temp**  
**chrome**

The following commands will list all blobs in the specified container, which start with **temp** and **chrome**:  
**abu -lf mylist.txt**  
**abu -listfile mylist.txt**  

The following commands will list all blobs and their sizes in the specified container, which start with **temp** and **chrome**:  
**abu -lfs mylist.txt**  
**abu -listfilesize mylist.txt**  

You can dump the blob listing to another file by redirecting the output:  
**abu -lf mylist.txt > myoutfile.txt**  
This is particularly useful when you want to delete some blobs.

### Delete blobs
Delete blobs option works on specific blobs 
