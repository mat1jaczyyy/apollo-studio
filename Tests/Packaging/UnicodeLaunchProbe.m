#import <Foundation/Foundation.h>
#import <unistd.h>

// A native launch control: no Apollo, .NET, Avalonia, or GUI automation.
int main(void) {
    @autoreleasepool {
        NSBundle *bundle = [NSBundle mainBundle];
        NSDictionary *record = @{
            @"bundle": bundle.bundlePath,
            @"executable": bundle.executablePath,
            @"identifier": bundle.bundleIdentifier,
            @"pid": @(getpid()),
            @"date": [[NSDate date] description]
        };
        NSData *json = [NSJSONSerialization dataWithJSONObject:record
            options:NSJSONWritingPrettyPrinted error:nil];
        NSString *name = [NSString stringWithFormat:@"launch-%d.json", getpid()];
        NSString *output = [[bundle.bundlePath stringByDeletingLastPathComponent]
            stringByAppendingPathComponent:name];
        return [json writeToFile:output atomically:YES] ? 0 : 1;
    }
}
