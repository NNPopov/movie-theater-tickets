import 'dart:io';

// import 'package:education_app_tutorial/core/common/features/video/data/models/video_model.dart';
// import 'package:education_app_tutorial/core/common/features/video/domain/entities/video.dart';
// import 'package:education_app_tutorial/core/extensions/string_extensions.dart';
// import 'package:file_picker/file_picker.dart';
import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

import '../res/colours.dart';
//import 'package:youtube_metadata/youtube.dart';


class Utils {
  // static Future<Video?> getVideoFromYT(BuildContext context, String url) async {
  //   try {
  //     final metaData = await YoutubeMetaData.getData(url);
  //
  //     debugPrint('title: ${metaData.title}');
  //     debugPrint('authorName: ${metaData.authorName}');
  //     debugPrint('authorUrl: ${metaData.authorUrl}');
  //     debugPrint('type: ${metaData.type}');
  //     debugPrint('height: ${metaData.height}');
  //     debugPrint('width: ${metaData.width}');
  //     debugPrint('version: ${metaData.version}');
  //     debugPrint('providerName: ${metaData.providerName}');
  //     debugPrint('providerUrl: ${metaData.providerUrl}');
  //     debugPrint('thumbnailHeight: ${metaData.thumbnailHeight}');
  //     debugPrint('thumbnailWidth: ${metaData.thumbnailWidth}');
  //     debugPrint('thumbnailUrl: ${metaData.thumbnailUrl}');
  //     debugPrint('html: ${metaData.html}');
  //     debugPrint('url: ${metaData.url}');
  //     debugPrint('description: ${metaData.description}');
  //     if (metaData.thumbnailUrl == null ||
  //         metaData.title == null ||
  //         metaData.authorName == null) {
  //       final message = 'Could not get video data. Please try again.\n'
  //           'The missing data is ${metaData.thumbnailUrl == null ? 'thu'
  //           'mb' : metaData.title == null ? 'title' : 'authorName'}';
  //       throw Exception(message);
  //     }
  //     return VideoModel.empty().copyWith(
  //       thumbnail: metaData.thumbnailUrl,
  //       videoURL: url,
  //       title: metaData.title,
  //       tutor: metaData.authorName,
  //     );
  //   } catch (e) {
  //     showSnackBar(context, 'PLEASE TRY AGAIN\n$e}');
  //   }
  //   return null;
  // }

  static void showLoadingDialog(BuildContext context) {
    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (context) {
        return const Center(
          child: CircularProgressIndicator(),
        );
      },
    );
  }

  static void showSnackBar(BuildContext context, String message) {
    ScaffoldMessenger.of(context)
      ..removeCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            message,
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.bold,
            ),
          ),
          behavior: SnackBarBehavior.floating,
          backgroundColor: Colours.primaryColour,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
          margin: const EdgeInsets.symmetric(
            horizontal: 10,
            vertical: 10,
          ),
        ),
      );
  }

  static void dismissKeyboard() {
    FocusManager.instance.primaryFocus?.unfocus();
  }

  // static Future<bool?> showConfirmationDialog(
  //     BuildContext context, {
  //       String? text,
  //       String? title,
  //       String? content,
  //       String? actionText,
  //       String? cancelText,
  //       Color? actionColor,
  //       Color? cancelColor,
  //     }) async {
  //   debugPrint('showConfirmationDialog');
  //   if (Theme.of(context).platform == TargetPlatform.iOS) {
  //     return showCupertinoDialog<bool>(
  //       context: context,
  //       builder: (context) {
  //         return CupertinoAlertDialog(
  //           title: Text(title ?? text!),
  //           content: Text(content ?? 'Are you sure you want to $text?'),
  //           actions: [
  //             CupertinoDialogAction(
  //               child: Text(
  //                 cancelText ?? 'Cancel',
  //                 style: TextStyle(color: cancelColor),
  //               ),
  //               onPressed: () {
  //                 Navigator.pop(context, false);
  //               },
  //             ),
  //             CupertinoDialogAction(
  //               child: Text(
  //                 actionText ?? text!.split(' ')[0].trim().titleCase,
  //                 style: TextStyle(color: actionColor),
  //               ),
  //               onPressed: () {
  //                 Navigator.pop(context, true);
  //               },
  //             ),
  //           ],
  //         );
  //       },
  //     );
  //   }
  //   return showDialog<bool>(
  //     context: context,
  //     builder: (context) {
  //       return AlertDialog(
  //         title: Text(title ?? text!),
  //         content: Text(content ?? 'Are you sure you want to $text?'),
  //         actions: [
  //           TextButton(
  //             onPressed: () {
  //               Navigator.pop(context, false);
  //             },
  //             child: Text(
  //               cancelText ?? 'Cancel',
  //               style: TextStyle(color: cancelColor),
  //             ),
  //           ),
  //           TextButton(
  //             onPressed: () {
  //               Navigator.pop(context, true);
  //             },
  //             child: Text(
  //               actionText ?? text!.split(' ')[0].trim().titleCase,
  //               style: TextStyle(color: actionColor),
  //             ),
  //           ),
  //         ],
  //       );
  //     },
  //   );
  // }

  // static Future<File?> pickCustomFile({
  //   List<String>? allowedExtensions,
  // }) async {
  //   final result = await FilePicker.platform.pickFiles(
  //     // The difference between FileType.any and FileType.custom is that
  //     // FileType.any will allow you to pick any file type
  //     // while FileType.custom will only allow you to pick the file types you
  //     // specify in the allowedExtensions parameter
  //     type: FileType.custom,
  //     allowedExtensions: allowedExtensions,
  //   );
  //   if (result != null) {
  //     return File(result.files.single.path!);
  //   }
  //   return null;
  // }
}
