#import <UIKit/UIKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

extern "C"
{
@interface WebAuthViewController : UIViewController <ASWebAuthenticationPresentationContextProviding>
{
    ASWebAuthenticationSession *authSession;
}
@end

@implementation WebAuthViewController

- (UIWindow *)getKeyWindow {
    if (@available(iOS 13.0, *)) {
        for (UIWindowScene* windowScene in [UIApplication sharedApplication].connectedScenes) {
            if (windowScene.activationState == UISceneActivationStateForegroundActive) {
                UIWindow *window = windowScene.windows.firstObject;
                if (window) {
                    return window;
                }
            }
        }
    } else {
        for (UIWindow *window in [UIApplication sharedApplication].windows) {
            if (window.isKeyWindow) {
                return window;
            }
        }
    }
    return nil;
}

- (UIWindow *)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session {
    return [self getKeyWindow];
}

WebAuthViewController * wvc;
typedef void (*AuthCallback)(const char *url, const char *error);

void openURL(const char * url, const char * callbackScheme, AuthCallback callback)
{
    NSLog(@"Launching ASWebAuthenticationSession with URL=%@", [[NSString alloc] initWithUTF8String:url]);
    NSURL * URL = [NSURL URLWithString: [[NSString alloc] initWithUTF8String:url]];
    NSString * scheme = [[NSString alloc] initWithUTF8String:callbackScheme];

    wvc = [[WebAuthViewController alloc] init];
    
    wvc->authSession = [[ASWebAuthenticationSession alloc] initWithURL:URL
                                               callbackURLScheme:scheme
                                               completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error) {
        if (error) {
            NSLog(@"ASWebAuthenticationSession Error: %@", error.localizedDescription);
            callback (nil, [error.localizedDescription UTF8String]);
        } else {
            NSLog(@"ASWebAuthenticationSession Success: %@", callbackURL.absoluteString);
            callback ([callbackURL.absoluteString UTF8String], nil);
        }
    }];
    wvc->authSession.presentationContextProvider = wvc;
    [wvc->authSession start];
}
void dismiss()
{
    if (wvc->authSession) {
        NSLog(@"Cancelling ASWebAuthenticationSession");
        [wvc->authSession cancel];
        wvc->authSession = nil;
    }
}
@end
}
